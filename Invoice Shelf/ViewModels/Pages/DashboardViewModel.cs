using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    public DashboardViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService   = apiService   ?? throw new ArgumentNullException(nameof(apiService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    // Au chargement de la page : on sert le cache s'il est encore valide (< 7 jours),
    // sinon on va chercher les données réseau. Le pull-to-refresh, lui, ignore
    // toujours le cache (voir RefreshCommand ci-dessous).
    internal async void Loaded(object? sender, EventArgs e) => await LoadAsync(forceRefresh: false);

    [ObservableProperty] private string _totalDue    = "–";
    [ObservableProperty] private string _totalPaid   = "–";
    [ObservableProperty] private int    _overdueCount;
    [ObservableProperty] private int    _draftCount;
    [ObservableProperty] private int    _sentCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecentInvoices))]
    private IEnumerable<Invoice> _recentInvoices = [];

    public bool HasRecentInvoices => RecentInvoices.Any();

    [ObservableProperty] private string _greetingName = string.Empty;
    [ObservableProperty] private bool   _isLoading;

    /// <summary>
    /// Charge le profil et les factures. Si <paramref name="forceRefresh"/> est faux et que des
    /// entrées de cache valides (moins de 7 jours) existent, elles sont utilisées directement sans
    /// appel réseau. Sinon (cache absent, périmé, ou rafraîchissement forcé), les données sont
    /// récupérées depuis l'API puis réécrites dans le cache.
    /// Le cache des factures (<see cref="CacheKeys.Invoices"/>) est partagé avec
    /// <see cref="InvoicesViewModel"/> : les deux pages restent cohérentes entre elles et
    /// un rafraîchissement sur l'une profite à l'autre.
    /// </summary>
    private async Task LoadAsync(bool forceRefresh)
    {
        if (!forceRefresh)
        {
            var cachedInvoices = await _cacheService.GetAsync<List<Invoice>>(CacheKeys.Invoices);
            if (cachedInvoices.IsFresh && cachedInvoices.Value is not null)
            {
                var cachedProfile = await _cacheService.GetAsync<UserProfile>(CacheKeys.Profile);
                if (cachedProfile.IsFresh && cachedProfile.Value is not null)
                    GreetingName = cachedProfile.Value.Name?.Split(' ').FirstOrDefault() ?? string.Empty;

                ApplyInvoices(cachedInvoices.Value);
                return;
            }
        }

        IsLoading = true;
        try
        {
            var profile = await _apiService.GetMe();
            GreetingName = profile?.Name?.Split(' ').FirstOrDefault() ?? string.Empty;
            if (profile is not null)
                await _cacheService.SetAsync(CacheKeys.Profile, profile);

            var invoices = await _apiService.GetInvoices();
            ApplyInvoices(invoices);
            await _cacheService.SetAsync(CacheKeys.Invoices, invoices);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur Dashboard : {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyInvoices(List<Invoice> invoices)
    {
        if (invoices.Count == 0)
            return;

        // Symbole de la devise depuis la première facture qui en a une
        string symbol = invoices
            .FirstOrDefault(i => i.Currency is not null)
            ?.Currency?.Symbol ?? string.Empty;

        decimal totalDue  = invoices.Where(i => i.Status != "COMPLETED").Sum(i => i.DueAmount) / 100m;
        decimal totalPaid = invoices.Where(i => i.Status == "COMPLETED").Sum(i => i.Total)     / 100m;

        TotalDue  = $"{symbol}{totalDue:N2}";
        TotalPaid = $"{symbol}{totalPaid:N2}";

        // Le retard est recalculé côté client (voir Invoice.IsOverdue) car le
        // serveur ne renvoie pas toujours status="OVERDUE" pour une facture
        // pourtant échue (le flag n'est mis à jour que par un job périodique).
        // On exclut donc les factures en retard des compteurs Brouillon/Envoyée
        // pour que les trois compteurs restent mutuellement exclusifs.
        OverdueCount = invoices.Count(i => i.IsOverdue);
        DraftCount   = invoices.Count(i => i.Status == "DRAFT" && !i.IsOverdue);
        SentCount    = invoices.Count(i => i.Status == "SENT"  && !i.IsOverdue);

        RecentInvoices = invoices
            .OrderByDescending(i => i.InvoiceDate)
            .Take(5)
            .ToList();
    }

    /// <summary>Déclenché par le pull-to-refresh : ignore systématiquement le cache.</summary>
    [RelayCommand]
    private async Task Refresh() => await LoadAsync(forceRefresh: true);

    [RelayCommand]
    private async Task OpenInvoice(Invoice invoice)
        => await Shell.Current.GoToAsync($"InvoiceDetailPage?invoiceId={invoice.Id}");
}
