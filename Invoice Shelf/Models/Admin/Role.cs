namespace InvoiceShelf.Models.Admin;

public record Ability(
    [property: JsonPropertyName("id")]           int     Id,
    [property: JsonPropertyName("name")]         string  Name,
    [property: JsonPropertyName("title")]        string  Title,
    [property: JsonPropertyName("entity_id")]    int?    EntityId,
    [property: JsonPropertyName("entity_type")]  string? EntityType,
    [property: JsonPropertyName("only_owned")]   bool    OnlyOwned,
    [property: JsonPropertyName("scope")]        int     Scope
);

public record Role(
    [property: JsonPropertyName("id")]                   int            Id,
    [property: JsonPropertyName("name")]                 string         Name,
    [property: JsonPropertyName("title")]                string         Title,
    [property: JsonPropertyName("level")]                int?           Level,
    [property: JsonPropertyName("formatted_created_at")] string?        FormattedCreatedAt,
    [property: JsonPropertyName("abilities")]            List<Ability>? Abilities
);
