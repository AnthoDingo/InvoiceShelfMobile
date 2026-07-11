namespace InvoiceShelf.Models.Admin;

public record Billing(
    [property: JsonPropertyName("id")]               int      Id,
    [property: JsonPropertyName("name")]             string?  Name,
    [property: JsonPropertyName("address_street_1")] string?  AddressStreet1,
    [property: JsonPropertyName("address_street_2")] string?  AddressStreet2,
    [property: JsonPropertyName("city")]             string?  City,
    [property: JsonPropertyName("state")]            string?  State,
    [property: JsonPropertyName("zip")]              string?  Zip,
    [property: JsonPropertyName("phone")]            string?  Phone,
    [property: JsonPropertyName("fax")]              string?  Fax,
    [property: JsonPropertyName("type")]             string?  Type,
    [property: JsonPropertyName("country_id")]       int      CountryId,
    [property: JsonPropertyName("user_id")]          int?     UserId,
    [property: JsonPropertyName("company_id")]       int?     CompanyId,
    [property: JsonPropertyName("customer_id")]      int?     CustomerId,
    [property: JsonPropertyName("country")]          Country? Country
);
