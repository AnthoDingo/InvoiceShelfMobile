namespace InvoiceShelf.Models.Admin;

public record Expenses(
    [property: JsonPropertyName("data")] List<Expense> Data,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null
) : IPaginatedResponse<Expense>;

public record ExpenseCategory(
    [property: JsonPropertyName("id")]          int     Id,
    [property: JsonPropertyName("name")]        string  Name,
    [property: JsonPropertyName("description")] string? Description
);

public record Expense(
    [property: JsonPropertyName("id")]                    int              Id,
    [property: JsonPropertyName("expense_date")]          string?          ExpenseDate,
    [property: JsonPropertyName("formatted_expense_date")] string?         FormattedExpenseDate,
    [property: JsonPropertyName("amount")]                decimal          Amount,
    [property: JsonPropertyName("base_amount")]           decimal          BaseAmount,
    [property: JsonPropertyName("notes")]                 string?          Notes,
    [property: JsonPropertyName("expense_category_id")]   int?             ExpenseCategoryId,
    [property: JsonPropertyName("company_id")]            int              CompanyId,
    [property: JsonPropertyName("customer_id")]           int?             CustomerId,
    [property: JsonPropertyName("currency_id")]           int              CurrencyId,
    [property: JsonPropertyName("expense_category")]      ExpenseCategory? ExpenseCategory,
    [property: JsonPropertyName("customer")]              Customer?        Customer,
    [property: JsonPropertyName("currency")]              Currency?        Currency
)
{
    public string CategoryName => ExpenseCategory?.Name ?? "Sans catégorie";

    public string FormattedAmount
    {
        get
        {
            decimal amount = Amount / 100m;
            return Currency is not null && !string.IsNullOrEmpty(Currency.Symbol)
                ? $"{Currency.Symbol}{amount:N2}"
                : $"{amount:N2}";
        }
    }
}

/// <summary>Réponse paginée de GET /api/v1/categories (catégories de dépenses).</summary>
public record ExpenseCategories(
    [property: JsonPropertyName("data")] List<ExpenseCategory> Data,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null
) : IPaginatedResponse<ExpenseCategory>;

/// <summary>Enveloppe "data" renvoyée par GET/POST /api/v1/expenses/{id}.</summary>
public record ExpenseDetail(
    [property: JsonPropertyName("data")] Expense? Data
);

/// <summary>
/// Payload envoyé à POST /api/v1/expenses pour créer une dépense.
/// Les champs suivent exactement ExpenseRequest côté API InvoiceShelf :
/// expense_date, expense_category_id, amount et currency_id sont requis ;
/// customer_id, payment_method_id et notes sont optionnels.
/// NB : "amount" est exprimé en centimes, comme partout ailleurs dans l'API.
/// La devise envoyée est celle de la société, ce qui dispense d'exchange_rate.
/// </summary>
public record CreateExpenseRequest(
    [property: JsonPropertyName("expense_date")]        string  ExpenseDate,
    [property: JsonPropertyName("expense_category_id")] int     ExpenseCategoryId,
    [property: JsonPropertyName("amount")]              long    Amount,
    [property: JsonPropertyName("currency_id")]         int     CurrencyId,
    [property: JsonPropertyName("customer_id")]         int?    CustomerId,
    [property: JsonPropertyName("payment_method_id")]   int?    PaymentMethodId,
    [property: JsonPropertyName("notes")]               string? Notes
);

/// <summary>
/// Réponse de GET /api/v1/company/settings?settings[]=currency :
/// l'identifiant (sous forme de chaîne) de la devise de la société.
/// </summary>
public record CompanyCurrencySetting(
    [property: JsonPropertyName("currency")] string? Currency
);
