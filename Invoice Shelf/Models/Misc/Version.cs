namespace InvoiceShelf.Models.Misc;

public record Version(
    [property: JsonPropertyName("version")] string Number,
    [property: JsonPropertyName("channel")] string Channel
);
