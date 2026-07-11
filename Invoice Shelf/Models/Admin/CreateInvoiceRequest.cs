namespace InvoiceShelf.Models.Admin;

/// <summary>Ligne d'article envoyée dans le payload de création de facture.</summary>
public record CreateInvoiceItemRequest(
    [property: JsonPropertyName("name")]        string  Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("quantity")]     decimal Quantity,
    [property: JsonPropertyName("price")]        long    Price
);

/// <summary>
/// Valeur d'un champ personnalisé envoyée dans le payload de création de
/// facture. Suit le format attendu par HasCustomFieldsTrait::addCustomFields
/// côté API : "id" est l'identifiant du CustomField (définition), "value" son
/// contenu (string, nombre ou booléen selon le type du champ — voir
/// getCustomFieldValueKey côté serveur).
/// </summary>
public record CreateInvoiceCustomFieldRequest(
    [property: JsonPropertyName("id")]    int     Id,
    [property: JsonPropertyName("value")] object? Value
);

/// <summary>
/// Payload envoyé à POST /api/v1/invoices pour créer une facture.
/// Suit InvoicesRequest côté API InvoiceShelf : invoice_date, customer_id,
/// invoice_number, discount, discount_val, sub_total, total, tax,
/// template_name et items sont requis.
/// NB : le serveur recalcule sub_total/total/tax à partir des lignes
/// d'articles (voir App\Support\DocumentTotals) — les valeurs envoyées ici
/// doivent simplement être numériquement cohérentes pour passer la validation.
/// Comme ailleurs dans l'API, les montants (discount_val, sub_total, total,
/// tax, price) sont exprimés en centimes ; "quantity" ne l'est pas.
/// </summary>
public record CreateInvoiceRequest(
    [property: JsonPropertyName("invoice_date")]   string  InvoiceDate,
    [property: JsonPropertyName("due_date")]       string? DueDate,
    [property: JsonPropertyName("customer_id")]    int     CustomerId,
    [property: JsonPropertyName("invoice_number")] string  InvoiceNumber,
    [property: JsonPropertyName("discount")]       decimal Discount,
    [property: JsonPropertyName("discount_val")]   long    DiscountValue,
    [property: JsonPropertyName("sub_total")]      long    SubTotal,
    [property: JsonPropertyName("total")]          long    Total,
    [property: JsonPropertyName("tax")]            long    Tax,
    [property: JsonPropertyName("template_name")]  string  TemplateName,
    [property: JsonPropertyName("notes")]          string? Notes,
    [property: JsonPropertyName("items")]          List<CreateInvoiceItemRequest> Items,
    // Champs personnalisés (définis côté serveur pour le type "Invoice"). Omis du
    // JSON si null/vide (voir ApiService.JsonOptions : DefaultIgnoreCondition =
    // WhenWritingNull) — le serveur n'exige "customFields" que si des champs
    // obligatoires existent, validés côté client avant l'envoi.
    [property: JsonPropertyName("customFields")]   List<CreateInvoiceCustomFieldRequest>? CustomFields = null
);
