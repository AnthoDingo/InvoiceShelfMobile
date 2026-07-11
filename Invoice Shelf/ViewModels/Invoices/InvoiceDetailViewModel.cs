using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Invoices;

[QueryProperty(nameof(InvoiceIdParam), "invoiceId")]
public partial class InvoiceDetailViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public InvoiceDetailViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private int _currentId;

    public string InvoiceIdParam
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
            {
                _currentId = id;
                Task.Run(() => LoadInvoice(id));
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    [NotifyPropertyChangedFor(nameof(HasNotes))]
    [NotifyPropertyChangedFor(nameof(CanRecordPayment))]
    private Invoice? _invoice;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasItems => Invoice?.Items?.Count > 0;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Invoice?.Notes);

    // On ne propose l'enregistrement d'un paiement que s'il reste un solde dû.
    public bool CanRecordPayment => Invoice is not null && Invoice.DueAmount > 0;

    private async Task LoadInvoice(int id)
    {
        IsLoading = true;
        try   { Invoice = await _apiService.GetInvoice(id); }
        catch (Exception ex) { Console.WriteLine($"Erreur chargement facture {id} : {ex.Message}"); }
        finally { IsLoading = false; }
    }

    /// <summary>Recharge la facture courante, ex. au retour de la page d'enregistrement de paiement.</summary>
    public async Task RefreshAsync()
    {
        if (_currentId > 0)
            await LoadInvoice(_currentId);
    }

    [RelayCommand]
    private async Task RecordPayment()
    {
        if (Invoice is null) return;
        await Shell.Current.GoToAsync($"RecordPaymentPage?invoiceId={Invoice.Id}");
    }

    [RelayCommand]
    private async Task GoBack() => await Shell.Current.GoToAsync("..");
}
