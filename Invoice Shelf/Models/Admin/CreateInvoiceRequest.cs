namespace InvoiceShelf.Models.Admin;

/// <summary>
/// Ligne d'article envoyée dans le payload de création de facture.
/// NB : côté API, DocumentItemService::createItems accède directement à
/// $item['discount_val'] et $item['tax'] (sans valeur par défaut) pour
/// calculer base_discount_val/base_tax — ces deux clés doivent donc toujours
/// être présentes dans le JSON, même à 0, sous peine d'une erreur
/// "Undefined array key". De plus, la colonne invoice_items.discount_type
/// est NOT NULL en base (sans valeur par défaut) : "discount_type" doit donc
/// aussi toujours être envoyé ("fixed" par défaut côté InvoiceShelf). Ce
/// formulaire ne gère pas encore de remise ou de taxe par article : elles
/// sont envoyées à 0 / "fixed".
/// </summary>
public record CreateInvoiceItemRequest(
    [property: JsonPropertyName("name")]          string  Name,
    [property: JsonPropertyName("description")]   string? Description,
    [property: JsonPropertyName("quantity")]       decimal Quantity,
    [property: JsonPropertyName("price")]          long    Price,
    [property: JsonPropertyName("discount")]       decimal Discount = 0,
    [property: JsonPropertyName("discount_type")]  string  DiscountType = "fixed",
    [property: JsonPropertyName("discount_val")]   long    DiscountValue = 0,
    [property: JsonPropertyName("tax")]            long    Tax = 0
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
/// IMPORTANT : "exchange_rate" doit être envoyé explicitement. Ce client
/// n'envoyant pas "currency_id", InvoicesRequest::getInvoicePayload compare
/// currency_id (absent → null) à la devise de la société ; comme elles
/// diffèrent presque toujours, le serveur retient $this->exchange_rate tel
/// quel. Sans ce champ, exchange_rate est stocké à null en base, ce qui fait
/// échouer la désérialisation côté mobile (InvoiceResource renvoie
/// exchange_rate: null, incompatible avec un decimal non-nullable). Ce
/// client ne gère pas le multi-devises (voir limitations connues) : 1 est
/// donc toujours correct ici.
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
    [property: JsonPropertyName("exchange_rate")]  decimal ExchangeRate = 1,
    // Champs personnalisés (définis côté serveur pour le type "Invoice"). Omis du
    // JSON si null/vide (voir ApiService.JsonOptions : DefaultIgnoreCondition =
    // WhenWritingNull) — le serveur n'exige "customFields" que si des champs
    // obligatoires existent, validés côté client avant l'envoi.
    [property: JsonPropertyName("customFields")]   List<CreateInvoiceCustomFieldRequest>? CustomFields = null
);
