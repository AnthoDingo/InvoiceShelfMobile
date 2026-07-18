using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Expenses;

/// <summary>
/// Formulaire de création d'une dépense.
/// Ouvert depuis ExpensesPage via la route "CreateExpensePage".
/// </summary>
public partial class CreateExpenseViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly ICacheService _cacheService;

    /// <summary>Devise de la société, requise par l'API pour créer une dépense.</summary>
    private int? _companyCurrencyId;

    private bool _isLoaded;

    public CreateExpenseViewModel(ApiService apiService, ICacheService cacheService)
    {
        _apiService   = apiService;
        _cacheService = cacheService;
    }

    internal async void Loaded(object? sender, EventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;
        await LoadAsync();
    }

    [ObservableProperty]
    private List<ExpenseCategory> _categories = [];

    [ObservableProperty]
    private ExpenseCategory? _selectedCategory;

    [ObservableProperty]
    private List<Customer> _customers = [];

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private List<PaymentMethod> _paymentMethods = [];

    [ObservableProperty]
    private PaymentMethod? _selectedPaymentMethod;

    [ObservableProperty]
    private string _amount = string.Empty;

    [ObservableProperty]
    private DateTime _expenseDate = DateTime.Today;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _errorMessage;

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // Catégories, clients, modes de paiement et devise société :
            // indépendants, chargés en parallèle.
            Task<List<ExpenseCategory>> categoriesTask     = _apiService.GetExpenseCategories();
            Task<List<Customer>>        customersTask      = _apiService.GetCustomers();
            Task<List<PaymentMethod>>   paymentMethodsTask = _apiService.GetPaymentMethods();
            Task<int?>                  currencyTask       = _apiService.GetCompanyCurrencyId();
            await Task.WhenAll(categoriesTask, customersTask, paymentMethodsTask, currencyTask);

            Categories         = categoriesTask.Result;
            Customers          = customersTask.Result;
            PaymentMethods     = paymentMethodsTask.Result;
            _companyCurrencyId = currencyTask.Result;

            if (Categories.Count == 0)
                ErrorMessage = "Aucune catégorie de dépense. Créez-en une d'abord dans les paramètres d'InvoiceShelf.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur de chargement : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (IsSaving) return;

        ErrorMessage = null;

        if (SelectedCategory is null)
        {
            ErrorMessage = "La catégorie est requise.";
            return;
        }

        // Le séparateur décimal saisi peut être "," ou "." selon la culture du clavier.
        string normalizedAmount = Amount.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalizedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amountValue)
            || amountValue <= 0)
        {
            ErrorMessage = "Montant invalide.";
            return;
        }

        if (_companyCurrencyId is null)
        {
            // Nouvelle tentative : la devise a pu échouer au premier chargement (réseau).
            _companyCurrencyId = await _apiService.GetCompanyCurrencyId();
            if (_companyCurrencyId is null)
            {
                ErrorMessage = "Impossible de déterminer la devise de la société. Vérifiez la connexion.";
                return;
            }
        }

        IsSaving = true;
        try
        {
            long amountInCents = (long)Math.Round(amountValue * 100m, MidpointRounding.AwayFromZero);

            CreateExpenseRequest request = new CreateExpenseRequest(
                ExpenseDate: ExpenseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ExpenseCategoryId: SelectedCategory.Id,
                Amount: amountInCents,
                CurrencyId: _companyCurrencyId.Value,
                CustomerId: SelectedCustomer?.Id,
                PaymentMethodId: SelectedPaymentMethod?.Id,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            );

            (Expense? expense, string? error) = await _apiService.CreateExpense(request);

            if (expense is null)
            {
                ErrorMessage = error ?? "Échec de la création de la dépense.";
                return;
            }

            // La liste des dépenses en cache est périmée : on l'invalide pour
            // forcer un rechargement réseau au retour sur l'onglet.
            await _cacheService.RemoveAsync(CacheKeys.Expenses);

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur réseau : {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task Cancel() => await Shell.Current.GoToAsync("..");
}
