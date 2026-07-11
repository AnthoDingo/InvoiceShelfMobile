namespace InvoiceShelf.Models.Admin;

public record Unit(
    [property: JsonPropertyName("id")]   int    Id,
    [property: JsonPropertyName("name")] string Name
);

/// <summary>Réponse paginée de GET /api/v1/items (catalogue d'articles de la société).</summary>
public record CatalogItems(
    [property: JsonPropertyName("data")] List<CatalogItem> Data,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null
) : IPaginatedResponse<CatalogItem>;

/// <summary>
/// Article du catalogue de la société (distinct des lignes d'articles d'une
/// facture, voir <see cref="Item"/>). Utilisé pour pré-remplir une ligne lors
/// de la création d'une facture. "Price" est exprimé en centimes.
/// </summary>
public record CatalogItem(
    [property: JsonPropertyName("id")]          int       Id,
    [property: JsonPropertyName("name")]        string    Name,
    [property: JsonPropertyName("description")] string?   Description,
    [property: JsonPropertyName("price")]       decimal   Price,
    [property: JsonPropertyName("unit_id")]     int?      UnitId,
    [property: JsonPropertyName("unit")]        Unit?     Unit,
    [property: JsonPropertyName("currency")]    Currency? Currency
);
