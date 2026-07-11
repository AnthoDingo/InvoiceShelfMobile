using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
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
    private readonly ICacheService _cacheService;

    public RecordPaymentViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService = apiService;
        _cacheService = cacheService;
    }

    public string InvoiceIdParam
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
                Task.Run(() => LoadAsync(id));
        }
    }

    [ObservableProperty]
    private Invoice? _invoice;

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
            ErrorMessage = $"Erreur de chargement : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Invoice is null || IsSaving) return;

        ErrorMessage = null;

        // Le séparateur décimal saisi peut être "," ou "." selon la culture du clavier.
        string normalizedAmount = Amount.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalizedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amountValue)
            || amountValue <= 0)
        {
            ErrorMessage = "Montant invalide.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PaymentNumber))
        {
            ErrorMessage = "Le numéro de paiement est requis.";
            return;
        }

        IsSaving = true;
        try
        {
            long amountInCents = (long)Math.Round(amountValue * 100m, MidpointRounding.AwayFromZero);

            var request = new CreatePaymentRequest(
                PaymentDate: PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CustomerId: Invoice.CustomerId,
                Amount: amountInCents,
                PaymentNumber: PaymentNumber.Trim(),
                InvoiceId: Invoice.Id,
                PaymentMethodId: SelectedPaymentMethod?.Id,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            );

            var (payment, error) = await _apiService.CreatePayment(request);

            if (payment is null)
            {
                ErrorMessage = error ?? "Échec de l'enregistrement du paiement.";
                return;
            }

            // La facture, ses paiements et la fiche client mise en cache sont désormais
            // périmés côté local : on les invalide pour forcer un rechargement réseau.
            await _cacheService.RemoveAsync(CacheKeys.Invoices);
            await _cacheService.RemoveAsync(CacheKeys.Payments);
            await _cacheService.RemoveAsync(CacheKeys.CustomerDetail(Invoice.CustomerId));

            await Shell.Current.GoToAsync("..");
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
}
