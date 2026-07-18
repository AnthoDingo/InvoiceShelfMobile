using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class PaymentsViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    public PaymentsViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService   = apiService;
        _cacheService = cacheService;
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
        if (!forceRefresh)
        {
            var cached = await _cacheService.GetAsync<List<Payment>>(CacheKeys.Payments);
            if (cached.IsFresh && cached.Value is not null)
            {
                Payments = cached.Value;
                return;
            }
        }

        IsRefreshing = true;
        try
        {
            var data = await _apiService.GetPayments();
            Payments = data;
            await _cacheService.SetAsync(CacheKeys.Payments, data);
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
