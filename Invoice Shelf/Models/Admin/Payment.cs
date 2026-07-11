namespace InvoiceShelf.Models.Admin;

public record Payments(
    [property: JsonPropertyName("data")] List<Payment> Data,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null
) : IPaginatedResponse<Payment>;

public record PaymentMethod(
    [property: JsonPropertyName("id")]   int    Id,
    [property: JsonPropertyName("name")] string Name
);

/// <summary>Réponse paginée de GET /api/v1/payment-methods (modes de paiement disponibles).</summary>
public record PaymentMethods(
    [property: JsonPropertyName("data")] List<PaymentMethod> Data,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null
) : IPaginatedResponse<PaymentMethod>;

/// <summary>Enveloppe "data" renvoyée par GET/POST /api/v1/payments/{id}.</summary>
public record PaymentDetail(
    [property: JsonPropertyName("data")] Payment? Data
);

/// <summary>
/// Payload envoyé à POST /api/v1/payments pour enregistrer un paiement.
/// Les champs suivent exactement PaymentRequest côté API InvoiceShelf :
/// payment_date, customer_id, amount et payment_number sont requis ;
/// invoice_id, payment_method_id et notes sont optionnels.
/// NB : "amount" est exprimé en centimes, comme partout ailleurs dans l'API.
/// </summary>
public record CreatePaymentRequest(
    [property: JsonPropertyName("payment_date")]      string  PaymentDate,
    [property: JsonPropertyName("customer_id")]       int     CustomerId,
    [property: JsonPropertyName("amount")]            long    Amount,
    [property: JsonPropertyName("payment_number")]    string  PaymentNumber,
    [property: JsonPropertyName("invoice_id")]        int?    InvoiceId,
    [property: JsonPropertyName("payment_method_id")] int?    PaymentMethodId,
    [property: JsonPropertyName("notes")]             string? Notes
);

/// <summary>Réponse de GET /api/v1/next-number?key=payment.</summary>
public record NextNumberResponse(
    [property: JsonPropertyName("success")]    bool    Success,
    [property: JsonPropertyName("nextNumber")] string? NextNumber
);

public record Payment(
    [property: JsonPropertyName("id")]                    int            Id,
    [property: JsonPropertyName("payment_number")]        string?        PaymentNumber,
    [property: JsonPropertyName("payment_date")]          string?        PaymentDate,
    [property: JsonPropertyName("formatted_payment_date")] string?       FormattedPaymentDate,
    [property: JsonPropertyName("notes")]                 string?        Notes,
    [property: JsonPropertyName("amount")]                decimal        Amount,
    [property: JsonPropertyName("base_amount")]           decimal        BaseAmount,
    [property: JsonPropertyName("exchange_rate")]         decimal        ExchangeRate,
    [property: JsonPropertyName("transaction_id")]        string?        TransactionId,
    [property: JsonPropertyName("customer_id")]           int            CustomerId,
    [property: JsonPropertyName("invoice_id")]            int?           InvoiceId,
    [property: JsonPropertyName("company_id")]            int            CompanyId,
    [property: JsonPropertyName("currency_id")]           int            CurrencyId,
    [property: JsonPropertyName("payment_method_id")]     int?           PaymentMethodId,
    [property: JsonPropertyName("customer")]              Customer?      Customer,
    [property: JsonPropertyName("invoice")]               Invoice?       Invoice,
    [property: JsonPropertyName("currency")]              Currency?      Currency,
    [property: JsonPropertyName("payment_method")]        PaymentMethod? PaymentMethod
)
{
    public string FormattedAmount
    {
        get
        {
            decimal amount = Amount / 100m;
            return Currency is not null && !string.IsNullOrEmpty(Currency.Symbol)
                ? $"{Currency.Symbol}{amount:N2}"
                : $"{amount:N2}";
        }
    }
}
