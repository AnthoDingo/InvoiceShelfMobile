using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class EstimatesViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public EstimatesViewModel(ApiService apiService)
    {
        _apiService   = apiService   ?? throw new ArgumentNullException(nameof(apiService));
    }

    // Au chargement de la page : on sert le cache s'il est encore valide (< 7 jours),
    // sinon on va chercher les données réseau. Le pull-to-refresh, lui, ignore
    // toujours le cache (voir RefreshCommand ci-dessous).
    internal async void Loaded(object? sender, EventArgs e) => await LoadAsync(forceRefresh: false);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Drafts))]
    [NotifyPropertyChangedFor(nameof(Sents))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IEnumerable<Estimate> _estimates = [];

    public IEnumerable<Estimate> Drafts  => Estimates.Where(e => e.Status == "DRAFT");
    public IEnumerable<Estimate> Sents   => Estimates.Where(e => e.Status == "SENT");
    public bool                  IsEmpty => !Estimates.Any();

    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>
    /// Charge les devis. Si <paramref name="forceRefresh"/> est faux et qu'une entrée
    /// de cache valide (moins de 7 jours) existe, elle est utilisée directement sans
    /// appel réseau. Sinon (cache absent, périmé, ou rafraîchissement forcé), les données
    /// sont récupérées depuis l'API puis réécrites dans le cache.
    /// </summary>
    private async Task LoadAsync(bool forceRefresh)
    {
        IsRefreshing = true;
        try
        {
            // Le cache (lecture, écriture, repli hors-ligne) est géré de façon
            // centralisée par ApiService : forceRefresh contourne le cache frais.
            List<Estimate> data = await _apiService.GetEstimates(forceRefresh);
            Estimates = data;
        }
        catch (Exception ex) { Console.WriteLine($"Erreur chargement devis : {ex.Message}"); }
        finally { IsRefreshing = false; }
    }

    /// <summary>Déclenché par le pull-to-refresh : ignore systématiquement le cache.</summary>
    [RelayCommand]
    private async Task Refresh() => await LoadAsync(forceRefresh: true);

    [RelayCommand]
    private async Task OpenEstimate(Estimate estimate)
        => await Shell.Current.GoToAsync($"EstimateDetailPage?estimateId={estimate.Id}");

    [RelayCommand]
    private async Task NewEstimate()
        => await Shell.Current.GoToAsync("CreateEstimatePage");
}
