using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Resources.Strings;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Invoices;

/// <summary>
/// Formulaire d'enregistrement d'un paiement pour une facture donnée.
/// Ouvert depuis InvoiceDetailPage via "RecordPaymentPage?invoiceId=...".
/// </summary>
[QueryProperty(nameof(InvoiceIdParam), "invoiceId")]
public partial class RecordPaymentViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public RecordPaymentViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    /// <summary>Vrai si la page a été ouverte avec un identifiant de facture.</summary>
    private bool _invoiceRequested;

    private bool _standaloneLoaded;

    public string InvoiceIdParam
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
            {
                _invoiceRequested = true;
                Task.Run(() => LoadAsync(id));
            }
        }
    }

    /// <summary>
    /// Sans facture passée en paramètre (ouverture depuis l'onglet Paiements),
    /// bascule en mode autonome : choix du client et, éventuellement, d'une
    /// facture impayée de ce client. Les paramètres de requête Shell étant
    /// appliqués avant l'événement Loaded, le test est fiable.
    /// </summary>
    internal async void Loaded(object? sender, EventArgs e)
    {
        if (_invoiceRequested || _standaloneLoaded) return;
        _standaloneLoaded = true;
        await LoadStandaloneAsync();
    }

    [ObservableProperty]
    private Invoice? _invoice;

    [ObservableProperty]
    private bool _isStandalone;

    [ObservableProperty]
    private List<Customer> _customers = [];

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private List<Invoice> _customerInvoices = [];

    [ObservableProperty]
    private Invoice? _selectedInvoice;

    /// <summary>Toutes les factures, pour filtrer par client en mode autonome.</summary>
    private List<Invoice> _allInvoices = [];

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        SelectedInvoice = null;
        CustomerInvoices = value is null
            ? []
            : _allInvoices.Where(i => i.CustomerId == value.Id && i.DueAmount > 0).ToList();
    }

    partial void OnSelectedInvoiceChanged(Invoice? value)
    {
        if (value is null) return;

        // Pré-remplit le montant avec le solde dû de la facture choisie.
        decimal due = value.DueAmount / 100m;
        if (due > 0)
            Amount = due.ToString("0.00", CultureInfo.InvariantCulture);
    }

    [ObservableProperty]
    private List<PaymentMethod> _paymentMethods = [];

    [ObservableProperty]
    private PaymentMethod? _selectedPaymentMethod;

    [ObservableProperty]
    private string _amount = string.Empty;

    [ObservableProperty]
    private DateTime _paymentDate = DateTime.Today;

    [ObservableProperty]
    private string _paymentNumber = string.Empty;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _errorMessage;

    public string? CurrencySymbol => Invoice?.Currency?.Symbol;

    private async Task LoadAsync(int invoiceId)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // Facture, modes de paiement et prochain numéro : indépendants, en parallèle.
            var invoiceTask       = _apiService.GetInvoice(invoiceId);
            var paymentMethodsTask = _apiService.GetPaymentMethods();
            var nextNumberTask    = _apiService.GetNextPaymentNumber();
            await Task.WhenAll(invoiceTask, paymentMethodsTask, nextNumberTask);

            Invoice = invoiceTask.Result;
            PaymentMethods = paymentMethodsTask.Result;
            PaymentNumber = nextNumberTask.Result ?? string.Empty;

            // Pré-remplit le montant avec le solde dû (stocké en centimes côté API).
            if (Invoice is not null)
            {
                decimal due = Invoice.DueAmount / 100m;
                Amount = due > 0
                    ? due.ToString("0.00", CultureInfo.InvariantCulture)
                    : string.Empty;
            }
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

    private async Task LoadStandaloneAsync()
    {
        IsStandalone = true;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // Clients, factures, modes de paiement et prochain numéro :
            // indépendants, chargés en parallèle.
            Task<List<Customer>>      customersTask      = _apiService.GetCustomers();
            Task<List<Invoice>>       invoicesTask       = _apiService.GetInvoices();
            Task<List<PaymentMethod>> paymentMethodsTask = _apiService.GetPaymentMethods();
            Task<string?>             nextNumberTask     = _apiService.GetNextPaymentNumber();
            await Task.WhenAll(customersTask, invoicesTask, paymentMethodsTask, nextNumberTask);

            Customers      = customersTask.Result;
            _allInvoices   = invoicesTask.Result;
            PaymentMethods = paymentMethodsTask.Result;
            PaymentNumber  = nextNumberTask.Result ?? string.Empty;
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

    [RelayCommand]
    private async Task Save()
    {
        if (IsSaving) return;

        ErrorMessage = null;

        // En mode autonome, le client vient du sélecteur ; sinon de la facture.
        int? customerId = Invoice?.CustomerId ?? SelectedCustomer?.Id;
        if (customerId is null)
        {
            ErrorMessage = AppStrings.Get("Common_SelectCustomerRequired");
            return;
        }

        // Le séparateur décimal saisi peut être "," ou "." selon la culture du clavier.
        string normalizedAmount = Amount.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalizedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amountValue)
            || amountValue <= 0)
        {
            ErrorMessage = AppStrings.Get("Common_InvalidAmount");
            return;
        }

        if (string.IsNullOrWhiteSpace(PaymentNumber))
        {
            ErrorMessage = AppStrings.Get("RecordPayment_NumberRequired");
            return;
        }

        IsSaving = true;
        try
        {
            long amountInCents = (long)Math.Round(amountValue * 100m, MidpointRounding.AwayFromZero);

            var request = new CreatePaymentRequest(
                PaymentDate: PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CustomerId: customerId.Value,
                Amount: amountInCents,
                PaymentNumber: PaymentNumber.Trim(),
                InvoiceId: Invoice?.Id ?? SelectedInvoice?.Id,
                PaymentMethodId: SelectedPaymentMethod?.Id,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            );

            var (payment, error) = await _apiService.CreatePayment(request);

            if (payment is null)
            {
                ErrorMessage = error ?? AppStrings.Get("RecordPayment_CreateFailedFallback");
                return;
            }

            // L'invalidation du cache est automatique : toute mutation réussie
            // (POST/PUT/DELETE) purge le cache GET dans ApiService.

            await Shell.Current.GoToAsync("..");
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
}
