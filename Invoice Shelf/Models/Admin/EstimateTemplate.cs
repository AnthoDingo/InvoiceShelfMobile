namespace InvoiceShelf.Models.Admin;

/// <summary>
/// Réponse de GET /api/v1/estimates/templates. NB : contrairement au reste de
/// l'API, la clé racine est en camelCase ("estimateTemplates").
/// L'aperçu image (base64, potentiellement volumineux) n'est pas mappé ici :
/// l'app ne propose qu'une sélection par nom.
/// </summary>
public record EstimateTemplatesResponse(
    [property: JsonPropertyName("estimateTemplates")] List<EstimateTemplate> EstimateTemplates
);

public record EstimateTemplate(
    [property: JsonPropertyName("name")]   string Name,
    [property: JsonPropertyName("custom")] bool   Custom
);
