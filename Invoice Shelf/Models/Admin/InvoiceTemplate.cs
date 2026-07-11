namespace InvoiceShelf.Models.Admin;

/// <summary>
/// Réponse de GET /api/v1/invoices/templates. NB : contrairement au reste de
/// l'API, la clé racine est en camelCase ("invoiceTemplates").
/// L'aperçu image (base64, potentiellement volumineux) n'est pas mappé ici :
/// l'app ne propose qu'une sélection par nom.
/// </summary>
public record InvoiceTemplatesResponse(
    [property: JsonPropertyName("invoiceTemplates")] List<InvoiceTemplate> InvoiceTemplates
);

public record InvoiceTemplate(
    [property: JsonPropertyName("name")]   string Name,
    [property: JsonPropertyName("custom")] bool   Custom
);
