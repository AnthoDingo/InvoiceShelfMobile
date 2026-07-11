namespace InvoiceShelf.Models.Admin;

public record Currency(
    [property: JsonPropertyName("id")]                 int      Id,
    [property: JsonPropertyName("name")]               string   Name,
    [property: JsonPropertyName("code")]               string   Code,
    [property: JsonPropertyName("symbol")]             string   Symbol,
    [property: JsonPropertyName("precision")]          int      Precision,
    [property: JsonPropertyName("thousand_separator")] string   ThousandSeparator,
    [property: JsonPropertyName("decimal_separator")]  string   DecimalSeparator,
    [property: JsonPropertyName("exchange_rate")]      decimal? ExchangeRate
);

public record Address(
    [property: JsonPropertyName("id")]               int     Id,
    [property: JsonPropertyName("name")]             string? Name,
    [property: JsonPropertyName("address_street_1")] string? AddressStreet1,
    [property: JsonPropertyName("address_street_2")] string? AddressStreet2,
    [property: JsonPropertyName("city")]             string? City,
    [property: JsonPropertyName("state")]            string? State,
    [property: JsonPropertyName("zip")]              string? Zip,
    [property: JsonPropertyName("phone")]            string? Phone,
    [property: JsonPropertyName("fax")]              string? Fax,
    [property: JsonPropertyName("type")]             string? Type,
    [property: JsonPropertyName("country_id")]       int     CountryId,
    [property: JsonPropertyName("user_id")]          int?    UserId,
    [property: JsonPropertyName("company_id")]       int?    CompanyId,
    [property: JsonPropertyName("customer_id")]      int?    CustomerId,
    [property: JsonPropertyName("country")]          Country? Country
);

public record CustomField(
    [property: JsonPropertyName("id")]             int          Id,
    [property: JsonPropertyName("name")]           string       Name,
    [property: JsonPropertyName("slug")]           string?      Slug,
    [property: JsonPropertyName("label")]          string?      Label,
    [property: JsonPropertyName("model_type")]     string?      ModelType,
    [property: JsonPropertyName("type")]           string?      Type,
    [property: JsonPropertyName("placeholder")]    string?      Placeholder,
    [property: JsonPropertyName("is_required")]    int          IsRequired,
    [property: JsonPropertyName("in_use")]         bool         InUse,
    [property: JsonPropertyName("order")]          int          Order,
    [property: JsonPropertyName("company_id")]     int          CompanyId,
    [property: JsonPropertyName("default_answer")] string?      DefaultAnswer,
    [property: JsonPropertyName("options")]        List<string>? Options
);

/// <summary>
/// Réponse de GET /api/v1/custom-fields?type={type}&limit=all : la liste
/// complète des définitions de champs personnalisés pour le type de modèle
/// demandé (ex. "Invoice"). Avec limit=all, le serveur renvoie une simple
/// collection ("data") sans pagination (voir CustomField::scopePaginateData).
/// </summary>
public record CustomFieldsResponse(
    [property: JsonPropertyName("data")] List<CustomField> Data
);

public record Field(
    [property: JsonPropertyName("id")]                          int          Id,
    [property: JsonPropertyName("type")]                        string?      Type,
    [property: JsonPropertyName("boolean_answer")]              bool?        BooleanAnswer,
    [property: JsonPropertyName("string_answer")]               string?      StringAnswer,
    [property: JsonPropertyName("number_answer")]               decimal?     NumberAnswer,
    [property: JsonPropertyName("date_answer")]                 string?      DateAnswer,
    [property: JsonPropertyName("default_answer")]              string?      DefaultAnswer,
    [property: JsonPropertyName("default_formatted_answer")]    string?      DefaultFormattedAnswer,
    [property: JsonPropertyName("custom_field_id")]             int          CustomFieldId,
    [property: JsonPropertyName("company_id")]                  int          CompanyId,
    [property: JsonPropertyName("custom_field")]                CustomField? CustomField,
    [property: JsonPropertyName("company")]                     Company?     Company
);

public record Creator(
    [property: JsonPropertyName("id")]           int           Id,
    [property: JsonPropertyName("name")]         string        Name,
    [property: JsonPropertyName("email")]        string        Email,
    [property: JsonPropertyName("phone")]        string?       Phone,
    [property: JsonPropertyName("role")]         string?       Role,
    [property: JsonPropertyName("is_owner")]     bool          IsOwner,
    [property: JsonPropertyName("roles")]        List<Role>?   Roles,
    [property: JsonPropertyName("companies")]    List<Company>? Companies
);

public record Company(
    [property: JsonPropertyName("id")]          int          Id,
    [property: JsonPropertyName("name")]        string       Name,
    [property: JsonPropertyName("vat_id")]      string?      VatId,
    [property: JsonPropertyName("tax_id")]      string?      TaxId,
    [property: JsonPropertyName("logo")]        string?      Logo,
    [property: JsonPropertyName("unique_hash")] string?      UniqueHash,
    [property: JsonPropertyName("owner_id")]    int          OwnerId,
    [property: JsonPropertyName("slug")]        string?      Slug,
    [property: JsonPropertyName("address")]     Address?     Address,
    [property: JsonPropertyName("roles")]       List<Role>?  Roles
);
