using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class ExpensesViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public ExpensesViewModel(ApiService apiService)
    {
        _apiService   = apiService;
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
        // Garde-fou de réentrance : RefreshView invoque son Command dès que
        // IsRefreshing passe à true, y compris quand c'est CE code qui vient
        // de le mettre à true (voir PaymentsViewModel pour le détail).
        if (IsRefreshing)
            return;

        IsRefreshing = true;
        try
        {
            // Le cache (lecture, écriture, repli hors-ligne) est géré de façon
            // centralisée par ApiService : forceRefresh contourne le cache frais.
            List<Expense> data = await _apiService.GetExpenses(forceRefresh);
            // L'API renvoie les dépenses par ordre de création croissant (les plus
            // anciennes d'abord) : on les trie ici pour afficher les plus récentes en tête.
            Expenses = SortLatestFirst(data);
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
