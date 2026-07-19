using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class CustomersViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    public CustomersViewModel(ApiService apiService)
    {
        _apiService   = apiService;
    }

    internal async void Loaded(object? sender, EventArgs e) => await LoadAsync(forceRefresh: false);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredCustomers))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IEnumerable<Customer> _customers = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredCustomers))]
    private string _searchText = string.Empty;

    public IEnumerable<Customer> FilteredCustomers
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return Customers;

            return Customers.Where(c =>
                (c.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase)        ?? false) ||
                (c.Email?.Contains(SearchText, StringComparison.OrdinalIgnoreCase)        ?? false) ||
                (c.CompanyName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase)  ?? false));
        }
    }

    public bool IsEmpty => !Customers.Any();

    [ObservableProperty]
    private bool _isRefreshing;

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
            List<Customer> data = await _apiService.GetCustomers(forceRefresh);
            Customers = data;
        }
        catch (Exception ex) { Console.WriteLine($"Erreur chargement clients : {ex.Message}"); }
        finally { IsRefreshing = false; }
    }

    /// <summary>Déclenché par le pull-to-refresh : ignore systématiquement le cache.</summary>
    [RelayCommand]
    private async Task Refresh() => await LoadAsync(forceRefresh: true);

    [RelayCommand]
    private async Task OpenCustomer(Customer customer)
        => await Shell.Current.GoToAsync($"CustomerDetailPage?customerId={customer.Id}");
}
