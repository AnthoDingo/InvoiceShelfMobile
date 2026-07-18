using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class ExpensesViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    public ExpensesViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService   = apiService;
        _cacheService = cacheService;
    }

    internal async void Loaded(object? sender, EventArgs e) => await LoadAsync(forceRefresh: false);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IEnumerable<Expense> _expenses = [];

    [ObservableProperty]
    private bool _isRefreshing;

    public bool IsEmpty => !Expenses.Any();

    private async Task LoadAsync(bool forceRefresh)
    {
        if (!forceRefresh)
        {
            var cached = await _cacheService.GetAsync<List<Expense>>(CacheKeys.Expenses);
            if (cached.IsFresh && cached.Value is not null)
            {
                Expenses = SortLatestFirst(cached.Value);
                return;
            }
        }

        IsRefreshing = true;
        try
        {
            var data = await _apiService.GetExpenses();
            // L'API renvoie les dépenses par ordre de création croissant (les plus
            // anciennes d'abord) : on les trie ici pour afficher les plus récentes en tête.
            var sorted = SortLatestFirst(data);
            Expenses = sorted;
            await _cacheService.SetAsync(CacheKeys.Expenses, sorted);
        }
        catch (Exception ex) { Console.WriteLine($"Erreur chargement dépenses : {ex.Message}"); }
        finally { IsRefreshing = false; }
    }

    /// <summary>Trie par identifiant décroissant, ce qui correspond à l'ordre de création
    /// (la dépense la plus récemment créée en premier).</summary>
    private static List<Expense> SortLatestFirst(IEnumerable<Expense> expenses)
        => expenses.OrderByDescending(e => e.Id).ToList();

    /// <summary>Déclenché par le pull-to-refresh : ignore systématiquement le cache.</summary>
    [RelayCommand]
    private async Task Refresh() => await LoadAsync(forceRefresh: true);

    /// <summary>Ouvre le formulaire de création d'une dépense.</summary>
    [RelayCommand]
    private async Task NewExpense() => await Shell.Current.GoToAsync("CreateExpensePage");
}
