using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Resources.Strings;
using InvoiceShelf.Services;
using InvoiceShelf.ViewModels.Invoices;

namespace InvoiceShelf.ViewModels.Estimates;

/// <summary>
/// Formulaire de création ou de modification d'un devis (toujours en
/// brouillon).
/// Création : ouvert depuis EstimatesPage via le bouton "+".
/// Édition : ouvert depuis EstimateDetailPage via le bouton "Modifier"
/// (visible uniquement sur un devis en brouillon), avec le paramètre de
/// requête "estimateId".
/// Réutilise InvoiceLineItemViewModel et CustomFieldInputViewModel (génériques,
/// partagés avec le formulaire de création de facture).
/// </summary>
[QueryProperty(nameof(EstimateIdParam), "estimateId")]
public partial class CreateEstimateViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    // Utilisé en repli si le serveur n'expose aucun template (ne devrait pas arriver).
    private const string FallbackTemplateName = "estimate1";

    private int? _editingEstimateId;

    public CreateEstimateViewModel(ApiService apiService)
    {
        _apiService = apiService;
        Items.CollectionChanged += (_, _) => RecalculateTotals();
        AddItem();
        Task.Run(LoadAsync);
    }

    /// <summary>
    /// Présent uniquement en mode édition (voir EstimateDetailViewModel.EditEstimate).
    /// Positionné par Shell avant que LoadAsync (déclenché dans le constructeur)
    /// n'ait eu le temps d'atteindre son dernier await, ce qui garantit que
    /// PopulateFromExisting s'exécute avec la liste des clients/templates déjà
    /// chargée.
    /// </summary>
    public string EstimateIdParam
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
            {
                _editingEstimateId = id;
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }
    }

    public bool IsEditMode => _editingEstimateId.HasValue;
    public string Title => IsEditMode ? AppStrings.Get("Estimate_EditTitle") : AppStrings.Get("Estimate_NewTitle");
    public string SaveButtonText => IsEditMode ? AppStrings.Get("Common_SaveChanges") : AppStrings.Get("Common_SaveAsDraft");

    [ObservableProperty]
    private List<Customer> _customers = [];

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private List<EstimateTemplate> _templates = [];

    [ObservableProperty]
    private EstimateTemplate? _selectedTemplate;

    [ObservableProperty]
    private List<CatalogItem> _catalogItems = [];

    public ObservableCollection<CustomFieldInputViewModel> CustomFields { get; } = [];

    /// <summary>Vrai si le serveur expose au moins un champ personnalisé pour les devis (section masquée sinon).</summary>
    public bool HasCustomFields => CustomFields.Count > 0;

    [ObservableProperty]
    private DateTime _estimateDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _expiryDate = DateTime.Today.AddDays(30);

    [ObservableProperty]
    private string _estimateNumber = string.Empty;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<InvoiceLineItemViewModel> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSubTotal))]
    [NotifyPropertyChangedFor(nameof(FormattedTotal))]
    private decimal _subTotal;

    public string? CurrencySymbol => SelectedCustomer?.Currency?.Symbol;
    public string FormattedSubTotal => Format(SubTotal);

    // Pas de remise ni de taxe gérées dans ce formulaire simplifié : le total = le sous-total.
    // (Le serveur recalcule de toute façon ces montants à partir des lignes d'articles.)
    public string FormattedTotal => Format(SubTotal);

    private string Format(decimal amount) =>
        string.IsNullOrEmpty(CurrencySymbol) ? amount.ToString("N2", CultureInfo.CurrentCulture) : $"{CurrencySymbol}{amount:N2}";

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        OnPropertyChanged(nameof(CurrencySymbol));
        OnPropertyChanged(nameof(FormattedSubTotal));
        OnPropertyChanged(nameof(FormattedTotal));
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Task<List<Customer>> customersTask          = _apiService.GetCustomers();
            Task<string?> nextNumberTask                = _apiService.GetNextEstimateNumber();
            Task<List<EstimateTemplate>> templatesTask   = _apiService.GetEstimateTemplates();
            Task<List<CatalogItem>> catalogItemsTask     = _apiService.GetCatalogItems();
            Task<List<CustomField>> customFieldsTask     = _apiService.GetCustomFields("Estimate");
            await Task.WhenAll(customersTask, nextNumberTask, templatesTask, catalogItemsTask, customFieldsTask);

            Customers = customersTask.Result;
            EstimateNumber = nextNumberTask.Result ?? string.Empty;
            Templates = templatesTask.Result;
            CatalogItems = catalogItemsTask.Result;

            // Champs personnalisés définis côté serveur pour les devis (Réglages
            // > Champs personnalisés d'InvoiceShelf). Triés par "order" comme sur le
            // front web (voir CreateCustomFields.vue).
            CustomFields.Clear();
            foreach (CustomField field in customFieldsTask.Result.OrderBy(f => f.Order))
                CustomFields.Add(new CustomFieldInputViewModel(field));
            OnPropertyChanged(nameof(HasCustomFields));

            // Présélectionne le template natif par défaut d'InvoiceShelf s'il existe,
            // sinon le premier template disponible.
            SelectedTemplate = Templates.FirstOrDefault(t => t.Name == FallbackTemplateName)
                ?? Templates.FirstOrDefault();

            if (_editingEstimateId is int editingId)
                await PopulateFromExisting(editingId);
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(AppStrings.Get("Common_LoadingErrorFormat"), ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Mode édition : pré-remplit le formulaire avec le devis existant. Appelée
    /// après le chargement des clients/templates/champs personnalisés (voir
    /// LoadAsync) afin de pouvoir sélectionner les bonnes entrées de ces listes.
    /// </summary>
    private async Task PopulateFromExisting(int id)
    {
        Estimate? estimate = await _apiService.GetEstimate(id);
        if (estimate is null)
        {
            ErrorMessage = AppStrings.Get("Estimate_LoadFailedMessage");
            return;
        }

        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == estimate.CustomerId);

        if (DateTime.TryParse(estimate.EstimateDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedEstimateDate))
            EstimateDate = parsedEstimateDate;

        if (DateTime.TryParse(estimate.ExpiryDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedExpiryDate))
            ExpiryDate = parsedExpiryDate;

        EstimateNumber = estimate.EstimateNumber ?? EstimateNumber;
        Notes = estimate.Notes;

        SelectedTemplate = Templates.FirstOrDefault(t => t.Name == estimate.TemplateName) ?? SelectedTemplate;

        Items.Clear();
        if (estimate.Items is { Count: > 0 } existingItems)
        {
            foreach (Item existingItem in existingItems)
            {
                InvoiceLineItemViewModel line = new InvoiceLineItemViewModel
                {
                    Name = existingItem.Name,
                    Description = existingItem.Description,
                    Quantity = existingItem.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
                    UnitPrice = (existingItem.Price / 100m).ToString("0.##", CultureInfo.InvariantCulture)
                };
                line.PropertyChanged += (_, _) => RecalculateTotals();
                Items.Add(line);
            }
        }
        else
        {
            AddItem();
        }
        RecalculateTotals();

        if (estimate.Fields is { Count: > 0 } existingFields)
        {
            foreach (Field existingField in existingFields)
            {
                CustomFieldInputViewModel? match = CustomFields.FirstOrDefault(f => f.Definition.Id == existingField.CustomFieldId);
                match?.ApplyExistingValue(existingField);
            }
        }
    }

    /// <summary>Recalcule le sous-total à partir des lignes d'articles actuelles.</summary>
    private void RecalculateTotals() => SubTotal = Items.Sum(i => i.LineTotal);

    [RelayCommand]
    private void AddItem()
    {
        InvoiceLineItemViewModel item = new InvoiceLineItemViewModel();
        // Une ligne change de valeurs (quantité/prix) : le sous-total doit se remettre à jour.
        item.PropertyChanged += (_, _) => RecalculateTotals();
        Items.Add(item);
    }

    [RelayCommand]
    private void RemoveItem(InvoiceLineItemViewModel item)
    {
        if (Items.Count <= 1) return; // toujours garder au moins une ligne
        Items.Remove(item);
        RecalculateTotals();
    }

    [RelayCommand]
    private async Task Save()
    {
        if (IsSaving) return;

        ErrorMessage = null;

        if (SelectedCustomer is null)
        {
            ErrorMessage = AppStrings.Get("Common_SelectCustomerRequired");
            return;
        }

        if (string.IsNullOrWhiteSpace(EstimateNumber))
        {
            ErrorMessage = AppStrings.Get("Estimate_NumberRequired");
            return;
        }

        string templateName = SelectedTemplate?.Name ?? FallbackTemplateName;

        List<InvoiceLineItemViewModel> validItems = Items.Where(i => i.IsValid).ToList();
        if (validItems.Count == 0)
        {
            ErrorMessage = AppStrings.Get("Common_ItemsRequired");
            return;
        }

        CustomFieldInputViewModel? invalidCustomField = CustomFields.FirstOrDefault(f => !f.IsValid);
        if (invalidCustomField is not null)
        {
            ErrorMessage = string.Format(AppStrings.Get("Common_CustomFieldRequiredFormat"), invalidCustomField.Label);
            return;
        }

        IsSaving = true;
        try
        {
            List<CreateEstimateItemRequest> itemRequests = validItems.Select(i => new CreateEstimateItemRequest(
                Name: i.Name.Trim(),
                Description: string.IsNullOrWhiteSpace(i.Description) ? null : i.Description.Trim(),
                Quantity: i.ParsedQuantity,
                Price: ToCents(i.ParsedUnitPrice),
                Discount: 0,
                DiscountType: "fixed",
                DiscountValue: 0,
                Tax: 0
            )).ToList();

            long subTotalCents = itemRequests.Sum(i => (long)Math.Round(i.Quantity * i.Price, MidpointRounding.AwayFromZero));

            // Un champ optionnel laissé vide n'est pas transmis (BuildValue() renvoie
            // null) : le serveur ne crée alors aucune CustomFieldValue pour lui.
            List<CreateEstimateCustomFieldRequest> customFieldRequests = CustomFields
                .Select(f => new CreateEstimateCustomFieldRequest(f.Definition.Id, f.BuildValue()))
                .Where(f => f.Value is not null)
                .ToList();

            CreateEstimateRequest request = new CreateEstimateRequest(
                EstimateDate: EstimateDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ExpiryDate: ExpiryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CustomerId: SelectedCustomer.Id,
                EstimateNumber: EstimateNumber.Trim(),
                Discount: 0,
                DiscountValue: 0,
                SubTotal: subTotalCents,
                Total: subTotalCents,
                Tax: 0,
                TemplateName: templateName,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                Items: itemRequests,
                ExchangeRate: 1,
                CustomFields: customFieldRequests.Count > 0 ? customFieldRequests : null
            );

            // Ne pas inclure "estimateSend" dans le payload : côté API, son absence
            // fait retomber le statut sur Estimate::STATUS_DRAFT (voir
            // EstimatesRequest::getEstimatePayload). Le devis est donc toujours
            // créé/conservé en brouillon depuis ce formulaire ; l'envoi au client se
            // fait ensuite séparément (POST /estimates/{id}/send).
            (Estimate? estimate, string? error) = _editingEstimateId is int editingId
                ? await _apiService.UpdateEstimate(editingId, request)
                : await _apiService.CreateEstimate(request);

            if (estimate is null)
            {
                ErrorMessage = error ?? (IsEditMode ? AppStrings.Get("Estimate_UpdateFailedFallback") : AppStrings.Get("Estimate_CreateFailedFallback"));
                return;
            }


            if (IsEditMode)
            {
                // On revient sur EstimateDetailPage (même instance) : son
                // OnAppearing/RefreshAsync recharge automatiquement le devis à jour.
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.GoToAsync("..");
                await Shell.Current.GoToAsync($"EstimateDetailPage?estimateId={estimate.Id}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(AppStrings.Get("Common_NetworkErrorFormat"), ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task Cancel() => await Shell.Current.GoToAsync("..");

    private static long ToCents(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
