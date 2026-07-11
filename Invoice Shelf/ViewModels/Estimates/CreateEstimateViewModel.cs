using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;
using InvoiceShelf.ViewModels.Invoices;

namespace InvoiceShelf.ViewModels.Estimates;

/// <summary>
/// Formulaire de création d'un nouveau devis (brouillon).
/// Ouvert depuis EstimatesPage via le bouton "+".
/// Réutilise InvoiceLineItemViewModel et CustomFieldInputViewModel (génériques,
/// partagés avec le formulaire de création de facture).
/// </summary>
public partial class CreateEstimateViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    // Utilisé en repli si le serveur n'expose aucun template (ne devrait pas arriver).
    private const string FallbackTemplateName = "estimate1";

    public CreateEstimateViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService = apiService;
        _cacheService = cacheService;
        Items.CollectionChanged += (_, _) => RecalculateTotals();
        AddItem();
        Task.Run(LoadAsync);
    }

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
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur de chargement : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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
            ErrorMessage = "Sélectionnez un client.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EstimateNumber))
        {
            ErrorMessage = "Le numéro de devis est requis.";
            return;
        }

        string templateName = SelectedTemplate?.Name ?? FallbackTemplateName;

        List<InvoiceLineItemViewModel> validItems = Items.Where(i => i.IsValid).ToList();
        if (validItems.Count == 0)
        {
            ErrorMessage = "Ajoutez au moins un article valide (nom, quantité et prix).";
            return;
        }

        CustomFieldInputViewModel? invalidCustomField = CustomFields.FirstOrDefault(f => !f.IsValid);
        if (invalidCustomField is not null)
        {
            ErrorMessage = $"Le champ « {invalidCustomField.Label} » est obligatoire.";
            return;
        }

        IsSaving = true;
        try
        {
            List<CreateEstimateItemRequest> itemRequests = validItems.Select(i => new CreateEstimateItemRequest(
                Name: i.Name.Trim(),
                Description: string.IsNullOrWhiteSpace(i.Description) ? null : i.Description.Trim(),
                Quantity: i.ParsedQuantity,
                Price: ToCents(i.ParsedUnitPrice)
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
                CustomFields: customFieldRequests.Count > 0 ? customFieldRequests : null
            );

            // Ne pas inclure "estimateSend" dans le payload : côté API, son absence
            // fait retomber le statut sur Estimate::STATUS_DRAFT (voir
            // EstimatesRequest::getEstimatePayload). Le devis est donc toujours
            // créé en brouillon depuis ce formulaire ; l'envoi au client se fait
            // ensuite séparément (POST /estimates/{id}/send).
            (Estimate? estimate, string? error) = await _apiService.CreateEstimate(request);

            if (estimate is null)
            {
                ErrorMessage = error ?? "Échec de la création du devis.";
                return;
            }

            // La liste des devis mise en cache est désormais périmée.
            await _cacheService.RemoveAsync(CacheKeys.Estimates);

            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync($"EstimateDetailPage?estimateId={estimate.Id}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur réseau : {ex.Message}";
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
