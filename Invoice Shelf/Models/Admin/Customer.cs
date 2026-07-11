namespace InvoiceShelf.Models.Admin;

public record Customers(
    [property: JsonPropertyName("data")] List<Customer> Data,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null
) : IPaginatedResponse<Customer>;

public record CustomerDetail(
    [property: JsonPropertyName("data")] Customer? Data
);

public record Customer(
    [property: JsonPropertyName("id")]                   int      Id,
    [property: JsonPropertyName("name")]                 string   Name,
    [property: JsonPropertyName("email")]                string?  Email,
    [property: JsonPropertyName("phone")]                string?  Phone,
    [property: JsonPropertyName("contact_name")]         string?  ContactName,
    [property: JsonPropertyName("company_name")]         string?  CompanyName,
    [property: JsonPropertyName("website")]              string?  Website,
    [property: JsonPropertyName("enable_portal")]        bool     EnablePortal,
    [property: JsonPropertyName("currency_id")]          int      CurrencyId,
    [property: JsonPropertyName("company_id")]           int      CompanyId,
    [property: JsonPropertyName("created_at")]           string?  CreatedAt,
    [property: JsonPropertyName("formatted_created_at")] string?  FormattedCreatedAt,
    [property: JsonPropertyName("due_amount")]           decimal? DueAmount,
    [property: JsonPropertyName("prefix")]               string?  Prefix,
    [property: JsonPropertyName("tax_id")]               string?  TaxId,
    [property: JsonPropertyName("billing")]              Billing?  Billing,
    [property: JsonPropertyName("currency")]             Currency? Currency,
    [property: JsonPropertyName("company")]              Company?  Company
)
{
    public string Initials
    {
        get
        {
            if (string.IsNullOrEmpty(Name)) return "?";
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : Name[0].ToString().ToUpper();
        }
    }

    public bool HasDueAmount => DueAmount.HasValue && DueAmount.Value != 0;

    public string FormattedDueAmount
    {
        get
        {
            decimal amount = (DueAmount ?? 0m) / 100m;
            return Currency is not null && !string.IsNullOrEmpty(Currency.Symbol)
                ? $"{Currency.Symbol}{amount:N2}"
                : $"{amount:N2}";
        }
    }
}
