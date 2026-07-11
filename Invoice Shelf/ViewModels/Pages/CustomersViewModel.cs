using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

public partial class CustomersViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    public CustomersViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService   = apiService;
        _cacheService = cacheService;
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
        if (!forceRefresh)
        {
            var cached = await _cacheService.GetAsync<List<Customer>>(CacheKeys.Customers);
            if (cached.IsFresh && cached.Value is not null)
            {
                Customers = cached.Value;
                return;
            }
        }

        IsRefreshing = true;
        try
        {
            var data = await _apiService.GetCustomers();
            Customers = data;
            await _cacheService.SetAsync(CacheKeys.Customers, data);
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
