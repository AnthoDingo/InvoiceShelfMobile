using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class PaymentsViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public PaymentsViewModel(ApiService apiService)
    {
        _apiService   = apiService;
    }

    internal async void Loaded(object? sender, EventArgs e) => await LoadAsync(forceRefresh: false);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IEnumerable<Payment> _payments = [];

    [ObservableProperty]
    private bool _isRefreshing;

    public bool IsEmpty => !Payments.Any();

    private async Task LoadAsync(bool forceRefresh)
    {
        IsRefreshing = true;
        try
        {
            // Le cache (lecture, écriture, repli hors-ligne) est géré de façon
            // centralisée par ApiService : forceRefresh contourne le cache frais.
            List<Payment> data = await _apiService.GetPayments(forceRefresh);
            Payments = data;
        }
        catch (Exception ex) { Console.WriteLine($"Erreur chargement paiements : {ex.Message}"); }
        finally { IsRefreshing = false; }
    }

    /// <summary>Déclenché par le pull-to-refresh : ignore systématiquement le cache.</summary>
    [RelayCommand]
    private async Task Refresh() => await LoadAsync(forceRefresh: true);

    /// <summary>Ouvre le formulaire d'enregistrement d'un paiement (mode autonome).</summary>
    [RelayCommand]
    private async Task RecordPayment() => await Shell.Current.GoToAsync("RecordPaymentPage");
}
