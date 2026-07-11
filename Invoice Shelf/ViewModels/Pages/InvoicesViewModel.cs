using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class InvoicesViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    public InvoicesViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService   = apiService   ?? throw new ArgumentNullException(nameof(apiService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    // Au chargement de la page : on sert le cache s'il est encore valide (< 7 jours),
    // sinon on va chercher les données réseau. Le pull-to-refresh, lui, ignore
    // toujours le cache (voir RefreshCommand ci-dessous).
    internal async void Loaded(object? sender, EventArgs e) => await LoadAsync(forceRefresh: false);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Drafts))]
    [NotifyPropertyChangedFor(nameof(Sents))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IEnumerable<Invoice> _invoices = [];

    public IEnumerable<Invoice> Drafts  => Invoices.Where(i => i.Status == "DRAFT");
    public IEnumerable<Invoice> Sents   => Invoices.Where(i => i.Status == "SENT");
    public bool                 IsEmpty => !Invoices.Any();

    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>
    /// Charge les factures. Si <paramref name="forceRefresh"/> est faux et qu'une entrée
    /// de cache valide (moins de 7 jours) existe, elle est utilisée directement sans
    /// appel réseau. Sinon (cache absent, périmé, ou rafraîchissement forcé), les données
    /// sont récupérées depuis l'API puis réécrites dans le cache.
    /// </summary>
    private async Task LoadAsync(bool forceRefresh)
    {
        if (!forceRefresh)
        {
            var cached = await _cacheService.GetAsync<List<Invoice>>(CacheKeys.Invoices);
            if (cached.IsFresh && cached.Value is not null)
            {
                Invoices = cached.Value;
                return;
            }
        }

        IsRefreshing = true;
        try
        {
            var data = await _apiService.GetInvoices();
            Invoices = data;
            await _cacheService.SetAsync(CacheKeys.Invoices, data);
        }
        catch (Exception ex) { Console.WriteLine($"Erreur chargement factures : {ex.Message}"); }
        finally { IsRefreshing = false; }
    }

    /// <summary>Déclenché par le pull-to-refresh : ignore systématiquement le cache.</summary>
    [RelayCommand]
    private async Task Refresh() => await LoadAsync(forceRefresh: true);

    [RelayCommand]
    private async Task OpenInvoice(Invoice invoice)
        => await Shell.Current.GoToAsync($"InvoiceDetailPage?invoiceId={invoice.Id}");

    [RelayCommand]
    private async Task NewInvoice()
        => await Shell.Current.GoToAsync("CreateInvoicePage");
}
