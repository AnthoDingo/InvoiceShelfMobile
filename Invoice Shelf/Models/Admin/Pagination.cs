namespace InvoiceShelf.Models.Admin;

/// <summary>
/// Métadonnées de pagination Laravel standard ("meta") renvoyées par les
/// endpoints de liste de l'API InvoiceShelf (invoices, estimates, expenses,
/// customers, payments...). Absente (null) sur les endpoints non paginés.
/// </summary>
public record PaginationMeta(
    [property: JsonPropertyName("current_page")] int CurrentPage,
    [property: JsonPropertyName("last_page")]    int LastPage
);

/// <summary>
/// Contrat implémenté par les réponses "liste" de l'API (Invoices, Estimates,
/// Expenses, Customers, Payments...) pour permettre à ApiService de parcourir
/// automatiquement toutes les pages via GetAllPages&lt;TResponse, TItem&gt;.
/// </summary>
public interface IPaginatedResponse<T>
{
    List<T> Data { get; }
    PaginationMeta? Meta { get; }
}
