namespace InvoiceShelf.Models.Admin;

public record Item(
    [property: JsonPropertyName("id")]                   int      Id,
    [property: JsonPropertyName("name")]                 string   Name,
    [property: JsonPropertyName("description")]          string?  Description,
    [property: JsonPropertyName("discount_type")]        string?  DiscountType,
    [property: JsonPropertyName("price")]                decimal  Price,
    [property: JsonPropertyName("quantity")]             decimal  Quantity,
    [property: JsonPropertyName("unit_name")]            string?  UnitName,
    [property: JsonPropertyName("discount")]             decimal  Discount,
    [property: JsonPropertyName("discount_val")]         decimal  DiscountValue,
    [property: JsonPropertyName("tax")]                  decimal  Tax,
    [property: JsonPropertyName("total")]                decimal  Total,
    // Nullable : absent des lignes de devis (EstimateItemResource n'expose que
    // "estimate_id", pas "invoice_id") et nullable en base pour les lignes de
    // facture issues d'une facture récurrente (voir migration invoice_items).
    [property: JsonPropertyName("invoice_id")]           int?     InvoiceId,
    // Nullable : "item_id" est la clé étrangère facultative vers le catalogue
    // d'articles (items table). Une ligne saisie manuellement (sans article de
    // catalogue sélectionné, ex. "Diagnostic") a item_id = null en base.
    [property: JsonPropertyName("item_id")]              int?     ItemId,
    // Nullable en base (voir migrations estimate_items/invoice_items).
    [property: JsonPropertyName("company_id")]           int?     CompanyId,
    [property: JsonPropertyName("base_price")]           decimal  BasePrice,
    // Nullable : peut être renvoyé null par l'API (aucune conversion appliquée).
    [property: JsonPropertyName("exchange_rate")]         decimal? ExchangeRate,
    [property: JsonPropertyName("base_discount_val")]    decimal  BaseDiscountValue,
    [property: JsonPropertyName("base_tax")]             decimal  BaseTax,
    [property: JsonPropertyName("base_total")]           decimal  BaseTotal,
    [property: JsonPropertyName("recurring_invoice_id")] int?     RecurringInvoiceId
)
{
    public string QuantityUnit => string.IsNullOrEmpty(UnitName)
        ? $"x{Quantity:G}"
        : $"{Quantity:G} {UnitName}";

    public string FormattedTotal => $"{Total / 100m:N2}";
}
