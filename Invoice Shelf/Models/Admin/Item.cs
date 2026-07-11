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
    [property: JsonPropertyName("invoice_id")]           int      InvoiceId,
    [property: JsonPropertyName("item_id")]              int      ItemId,
    [property: JsonPropertyName("company_id")]           int      CompanyId,
    [property: JsonPropertyName("base_price")]           decimal  BasePrice,
    [property: JsonPropertyName("exchange_rate")]         decimal  ExchangeRate,
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
