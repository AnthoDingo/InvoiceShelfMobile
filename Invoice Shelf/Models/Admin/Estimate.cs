using InvoiceShelf.Resources.Strings;

namespace InvoiceShelf.Models.Admin;

public record Estimates(
    [property: JsonPropertyName("data")] List<Estimate> Data,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null
) : IPaginatedResponse<Estimate>;

public record EstimateDetail(
    [property: JsonPropertyName("data")] Estimate? Data
);

public record Estimate(
    [property: JsonPropertyName("id")]                      int            Id,
    [property: JsonPropertyName("estimate_date")]           string?        EstimateDate,
    [property: JsonPropertyName("estimate_number")]         string?        EstimateNumber,
    [property: JsonPropertyName("expiry_date")]             string?        ExpiryDate,
    [property: JsonPropertyName("reference_number")]        string?        ReferenceNumber,
    [property: JsonPropertyName("status")]                  string         Status,
    [property: JsonPropertyName("notes")]                   string?        Notes,
    [property: JsonPropertyName("discount_type")]           string?        DiscountType,
    [property: JsonPropertyName("discount")]                decimal        Discount,
    [property: JsonPropertyName("discount_val")]            decimal        DiscountValue,
    [property: JsonPropertyName("sub_total")]               decimal        SubTotal,
    [property: JsonPropertyName("total")]                   decimal        Total,
    [property: JsonPropertyName("tax")]                     decimal        Tax,
    [property: JsonPropertyName("sequence_number")]         int            SequenceNumber,
    // Nullable : peut être renvoyé null par l'API (aucune conversion appliquée).
    [property: JsonPropertyName("exchange_rate")]           decimal?       ExchangeRate,
    [property: JsonPropertyName("base_sub_total")]          decimal        BaseSubTotal,
    [property: JsonPropertyName("base_total")]              decimal        BaseTotal,
    [property: JsonPropertyName("base_tax")]                decimal        BaseTax,
    [property: JsonPropertyName("base_discount_val")]       decimal        BaseDiscountValue,
    [property: JsonPropertyName("customer_id")]             int            CustomerId,
    [property: JsonPropertyName("currency_id")]             int?           CurrencyId,
    [property: JsonPropertyName("creator_id")]              int?           CreatorId,
    [property: JsonPropertyName("unique_hash")]             string?        UniqueHash,
    [property: JsonPropertyName("template_name")]           string?        TemplateName,
    [property: JsonPropertyName("estimate_pdf_url")]        string?        EstimatePdfUrl,
    [property: JsonPropertyName("formatted_estimate_date")] string?       FormattedEstimateDate,
    [property: JsonPropertyName("formatted_expiry_date")]   string?       FormattedExpiryDate,
    [property: JsonPropertyName("formatted_created_at")]    string?        FormattedCreatedAt,
    [property: JsonPropertyName("sales_tax_type")]          string?        SalesTaxType,
    [property: JsonPropertyName("items")]                   List<Item>?    Items,
    [property: JsonPropertyName("customer")]                Customer?      Customer,
    [property: JsonPropertyName("creator")]                 Creator?       Creator,
    [property: JsonPropertyName("fields")]                  List<Field>?   Fields,
    [property: JsonPropertyName("company")]                 Company?       Company,
    [property: JsonPropertyName("currency")]                Currency?      Currency
)
{
    // ── Statut ────────────────────────────────────────────────────────────
    // Valeurs possibles côté InvoiceShelf : DRAFT, SENT, VIEWED, ACCEPTED, REJECTED, EXPIRED.

    public string FormattedStatus => Status switch
    {
        "DRAFT"    => AppStrings.Get("Status_Draft"),
        "SENT"     => AppStrings.Get("Status_EstimateSent"),
        "VIEWED"   => AppStrings.Get("Status_Viewed"),
        "ACCEPTED" => AppStrings.Get("Status_Accepted"),
        "REJECTED" => AppStrings.Get("Status_Rejected"),
        "EXPIRED"  => AppStrings.Get("Status_Expired"),
        _          => Status
    };

    public Color StatusColor => Status switch
    {
        "DRAFT"    => Color.FromArgb("#94A3B8"),
        "SENT"     => Color.FromArgb("#F59E0B"),
        "VIEWED"   => Color.FromArgb("#3B82F6"),
        "ACCEPTED" => Color.FromArgb("#10B981"),
        "REJECTED" => Color.FromArgb("#EF4444"),
        "EXPIRED"  => Color.FromArgb("#64748B"),
        _          => Color.FromArgb("#64748B")
    };

    // ── Montants formatés ─────────────────────────────────────────────────

    public string FormattedTotal    => FormatAmount(Total);
    public string FormattedSubTotal => FormatAmount(SubTotal);
    public string FormattedTax      => FormatAmount(Tax);
    public string FormattedDiscount => DiscountValue == 0 ? "–" : $"-{FormatAmount(DiscountValue)}";

    private string FormatAmount(decimal centimes)
    {
        decimal amount = centimes / 100m;
        return Currency is not null && !string.IsNullOrEmpty(Currency.Symbol)
            ? $"{Currency.Symbol}{amount:N2}"
            : $"{amount:N2}";
    }
}
