using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public DashboardViewModel(ApiService apiService)
    {
        _apiService   = apiService   ?? throw new ArgumentNullException(nameof(apiService));
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
    /// Charge le profil et les factures. Le cache est géré de façon centralisée
    /// par ApiService (une entrée par endpoint GET) : le tableau de bord et la
    /// page Factures partagent donc naturellement les mêmes données locales, et
    /// un rafraîchissement sur l'une profite à l'autre.
    /// </summary>
    private async Task LoadAsync(bool forceRefresh)
    {
        IsLoading = true;
        try
        {
            // Le cache (lecture, écriture, repli hors-ligne) est géré de façon
            // centralisée par ApiService : forceRefresh contourne le cache frais.
            UserProfile? profile = await _apiService.GetMe(forceRefresh);
            GreetingName = profile?.Name?.Split(' ').FirstOrDefault() ?? string.Empty;

            List<Invoice> invoices = await _apiService.GetInvoices(forceRefresh);
            ApplyInvoices(invoices);
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
