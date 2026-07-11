using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Invoices;

/// <summary>
/// Formulaire de création ou de modification d'une facture (toujours en
/// brouillon).
/// Création : ouvert depuis InvoicesPage via le bouton "+".
/// Édition : ouvert depuis InvoiceDetailPage via le bouton "Modifier"
/// (visible uniquement sur une facture en brouillon), avec le paramètre de
/// requête "invoiceId".
/// </summary>
[QueryProperty(nameof(InvoiceIdParam), "invoiceId")]
public partial class CreateInvoiceViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    // Utilisé en repli si le serveur n'expose aucun template (ne devrait pas arriver).
    private const string FallbackTemplateName = "invoice1";

    private int? _editingInvoiceId;

    public CreateInvoiceViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService = apiService;
        _cacheService = cacheService;
        Items.CollectionChanged += (_, _) => RecalculateTotals();
        AddItem();
        Task.Run(LoadAsync);
    }

    /// <summary>
    /// Présent uniquement en mode édition (voir InvoiceDetailViewModel.EditInvoice).
    /// Positionné par Shell avant que LoadAsync (déclenché dans le constructeur)
    /// n'ait eu le temps d'atteindre son dernier await, ce qui garantit que
    /// PopulateFromExisting s'exécute avec la liste des clients/templates déjà
    /// chargée.
    /// </summary>
    public string InvoiceIdParam
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
            {
                _editingInvoiceId = id;
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }
    }

    public bool IsEditMode => _editingInvoiceId.HasValue;
    public string Title => IsEditMode ? "Modifier la facture" : "Nouvelle facture";
    public string SaveButtonText => IsEditMode ? "Enregistrer les modifications" : "Enregistrer en brouillon";

    [ObservableProperty]
    private List<Customer> _customers = [];

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private List<InvoiceTemplate> _templates = [];

    [ObservableProperty]
    private InvoiceTemplate? _selectedTemplate;

    [ObservableProperty]
    private List<CatalogItem> _catalogItems = [];

    public ObservableCollection<CustomFieldInputViewModel> CustomFields { get; } = [];

    /// <summary>Vrai si le serveur expose au moins un champ personnalisé pour les factures (section masquée sinon).</summary>
    public bool HasCustomFields => CustomFields.Count > 0;

    [ObservableProperty]
    private DateTime _invoiceDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _dueDate = DateTime.Today.AddDays(30);

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

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
            Task<List<Customer>> customersTask    = _apiService.GetCustomers();
            Task<string?> nextNumberTask          = _apiService.GetNextInvoiceNumber();
            Task<List<InvoiceTemplate>> templatesTask   = _apiService.GetInvoiceTemplates();
            Task<List<CatalogItem>> catalogItemsTask    = _apiService.GetCatalogItems();
            Task<List<CustomField>> customFieldsTask    = _apiService.GetCustomFields("Invoice");
            await Task.WhenAll(customersTask, nextNumberTask, templatesTask, catalogItemsTask, customFieldsTask);

            Customers = customersTask.Result;
            InvoiceNumber = nextNumberTask.Result ?? string.Empty;
            Templates = templatesTask.Result;
            CatalogItems = catalogItemsTask.Result;

            // Champs personnalisés définis côté serveur pour les factures (Réglages
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

            if (_editingInvoiceId is int editingId)
                await PopulateFromExisting(editingId);
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

    /// <summary>
    /// Mode édition : pré-remplit le formulaire avec la facture existante.
    /// Appelée après le chargement des clients/templates/champs personnalisés
    /// (voir LoadAsync) afin de pouvoir sélectionner les bonnes entrées de ces
    /// listes.
    /// </summary>
    private async Task PopulateFromExisting(int id)
    {
        Invoice? invoice = await _apiService.GetInvoice(id);
        if (invoice is null)
        {
            ErrorMessage = "Impossible de charger la facture à modifier.";
            return;
        }

        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == invoice.CustomerId);

        if (DateTime.TryParse(invoice.InvoiceDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedInvoiceDate))
            InvoiceDate = parsedInvoiceDate;

        if (DateTime.TryParse(invoice.DueDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDueDate))
            DueDate = parsedDueDate;

        InvoiceNumber = invoice.InvoiceNumber ?? InvoiceNumber;
        Notes = invoice.Notes;

        SelectedTemplate = Templates.FirstOrDefault(t => t.Name == invoice.TemplateName) ?? SelectedTemplate;

        Items.Clear();
        if (invoice.Items is { Count: > 0 } existingItems)
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

        if (invoice.Fields is { Count: > 0 } existingFields)
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
            ErrorMessage = "Sélectionnez un client.";
            return;
        }

        if (string.IsNullOrWhiteSpace(InvoiceNumber))
        {
            ErrorMessage = "Le numéro de facture est requis.";
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
            List<CreateInvoiceItemRequest> itemRequests = validItems.Select(i => new CreateInvoiceItemRequest(
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
            List<CreateInvoiceCustomFieldRequest> customFieldRequests = CustomFields
                .Select(f => new CreateInvoiceCustomFieldRequest(f.Definition.Id, f.BuildValue()))
                .Where(f => f.Value is not null)
                .ToList();

            CreateInvoiceRequest request = new CreateInvoiceRequest(
                InvoiceDate: InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DueDate: DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CustomerId: SelectedCustomer.Id,
                InvoiceNumber: InvoiceNumber.Trim(),
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

            // Ne pas inclure "invoiceSend" dans le payload : côté API, son absence
            // fait retomber le statut sur Invoice::STATUS_DRAFT (voir
            // InvoicesRequest::getInvoicePayload). La facture est donc toujours
            // créée/conservée en brouillon depuis ce formulaire ; l'envoi au
            // client se fait ensuite séparément (POST /invoices/{id}/send).
            (Invoice? invoice, string? error) = _editingInvoiceId is int editingId
                ? await _apiService.UpdateInvoice(editingId, request)
                : await _apiService.CreateInvoice(request);

            if (invoice is null)
            {
                ErrorMessage = error ?? (IsEditMode ? "Échec de la mise à jour de la facture." : "Échec de la création de la facture.");
                return;
            }

            // La liste des factures mise en cache est désormais périmée.
            await _cacheService.RemoveAsync(CacheKeys.Invoices);

            if (IsEditMode)
            {
                // On revient sur InvoiceDetailPage (même instance) : son
                // OnAppearing/RefreshAsync (déjà en place pour le retour depuis
                // RecordPaymentPage) recharge automatiquement la facture à jour.
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.GoToAsync("..");
                await Shell.Current.GoToAsync($"InvoiceDetailPage?invoiceId={invoice.Id}");
            }
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
