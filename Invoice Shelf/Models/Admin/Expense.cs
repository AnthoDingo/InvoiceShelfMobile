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
