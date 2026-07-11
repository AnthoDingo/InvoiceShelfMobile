namespace InvoiceShelf.Models.Admin;

public record Country(
    [property: JsonPropertyName("id")]         int    Id,
    [property: JsonPropertyName("code")]       string Code,
    [property: JsonPropertyName("name")]       string Name,
    [property: JsonPropertyName("phone_code")] string PhoneCode
);
