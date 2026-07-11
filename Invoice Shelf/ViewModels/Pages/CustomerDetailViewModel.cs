using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Pages;

[QueryProperty(nameof(CustomerIdParam), "customerId")]
public partial class CustomerDetailViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    public CustomerDetailViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService = apiService;
        _cacheService = cacheService;
    }

    public string CustomerIdParam
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
                Task.Run(() => LoadCustomer(id));
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedDueAmount))]
    private Customer? _customer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInvoices))]
    [NotifyPropertyChangedFor(nameof(InvoiceCount))]
    [NotifyPropertyChangedFor(nameof(FormattedDueAmount))]
    private IEnumerable<Invoice> _invoices = [];

    [ObservableProperty]
    private bool _isLoading;

    public bool HasInvoices  => Invoices.Any();

    // IEnumerable<T> n'a pas de propriété "Count" bindable en XAML (seulement la
    // méthode d'extension Count()) : on l'expose ici comme propriété calculée.
    public int InvoiceCount => Invoices.Count();

    // Le champ "due_amount" renvoyé par GetCustomer(id) peut être absent ou
    // désynchronisé (ex. cache serveur) alors que les factures elles-mêmes
    // portent chacune un "due_amount" à jour. On recalcule donc le solde dû
    // localement à partir des factures déjà chargées, ce qui reflète fidèlement
    // ce qui est affiché dans la liste (ex. factures "Envoyée" impayées).
    public decimal TotalDueAmount => Invoices.Sum(i => i.DueAmount);

    public string FormattedDueAmount
    {
        get
        {
            decimal amount = TotalDueAmount / 100m;
            var symbol = Customer?.Currency?.Symbol;
            return !string.IsNullOrEmpty(symbol)
                ? $"{symbol}{amount:N2}"
                : $"{amount:N2}";
        }
    }

    private async Task LoadCustomer(int id)
    {
        // Sert le cache si à la fois la fiche client et la liste des factures
        // sont encore fraîches (< 7 jours) : évite un appel réseau au premier
        // affichage. La liste des factures réutilise la même clé de cache que
        // la page Factures, pour ne pas dupliquer les données en local.
        var cachedCustomer = await _cacheService.GetAsync<Customer>(CacheKeys.CustomerDetail(id));
        var cachedInvoices = await _cacheService.GetAsync<List<Invoice>>(CacheKeys.Invoices);

        if (cachedCustomer.IsFresh && cachedCustomer.Value is not null &&
            cachedInvoices.IsFresh && cachedInvoices.Value is not null)
        {
            Customer = cachedCustomer.Value;
            Invoices = cachedInvoices.Value
                .Where(i => i.CustomerId == id)
                .OrderByDescending(i => i.InvoiceDate);
            return;
        }

        IsLoading = true;
        try
        {
            // Les deux appels sont indépendants : on les lance en parallèle.
            var invoicesTask = _apiService.GetInvoices();
            var customerTask = _apiService.GetCustomer(id);
            await Task.WhenAll(invoicesTask, customerTask);

            List<Invoice> allInvoices = invoicesTask.Result;
            Invoices = allInvoices
                .Where(i => i.CustomerId == id)
                .OrderByDescending(i => i.InvoiceDate);

            // Le client "complet" (avec due_amount, billing, etc.) vient de
            // GetCustomer(id) : l'objet "customer" imbriqué dans une facture est
            // partiel et ne contient pas le solde dû, d'où le champ vide observé.
            Customer = customerTask.Result
                ?? Invoices.FirstOrDefault()?.Customer;

            await _cacheService.SetAsync(CacheKeys.Invoices, allInvoices);
            if (Customer is not null)
                await _cacheService.SetAsync(CacheKeys.CustomerDetail(id), Customer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur chargement client {id} : {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenInvoice(Invoice invoice)
        => await Shell.Current.GoToAsync($"InvoiceDetailPage?invoiceId={invoice.Id}");

    [RelayCommand]
    private async Task GoBack() => await Shell.Current.GoToAsync("..");
}
