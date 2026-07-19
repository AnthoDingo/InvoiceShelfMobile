using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using InvoiceShelf.Models.Admin;
using InvoiceShelf.Resources.Strings;
using InvoiceShelf.Services;

namespace InvoiceShelf.ViewModels.Expenses;

/// <summary>
/// Formulaire de création d'une dépense.
/// Ouvert depuis ExpensesPage via la route "CreateExpensePage".
/// </summary>
public partial class CreateExpenseViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    /// <summary>Devise de la société, requise par l'API pour créer une dépense.</summary>
    private int? _companyCurrencyId;

    private bool _isLoaded;

    public CreateExpenseViewModel(ApiService apiService)
    {
        _apiService   = apiService;
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
                ErrorMessage = AppStrings.Get("Expense_NoCategoriesMessage");
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(AppStrings.Get("Common_LoadingErrorFormat"), ex.Message);
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
            ErrorMessage = AppStrings.Get("Expense_CategoryRequired");
            return;
        }

        // Le séparateur décimal saisi peut être "," ou "." selon la culture du clavier.
        string normalizedAmount = Amount.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalizedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amountValue)
            || amountValue <= 0)
        {
            ErrorMessage = AppStrings.Get("Common_InvalidAmount");
            return;
        }

        if (_companyCurrencyId is null)
        {
            // Nouvelle tentative : la devise a pu échouer au premier chargement (réseau).
            _companyCurrencyId = await _apiService.GetCompanyCurrencyId();
            if (_companyCurrencyId is null)
            {
                ErrorMessage = AppStrings.Get("Expense_CurrencyUnavailable");
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
                ErrorMessage = error ?? AppStrings.Get("Expense_CreateFailedFallback");
                return;
            }

            // L'invalidation du cache est automatique : toute mutation réussie
            // (POST/PUT/DELETE) purge le cache GET dans ApiService.

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(AppStrings.Get("Common_NetworkErrorFormat"), ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task Cancel() => await Shell.Current.GoToAsync("..");
}
