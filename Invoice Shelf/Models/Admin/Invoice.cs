using InvoiceShelf.Resources.Strings;

namespace InvoiceShelf.Models.Admin;

public record Invoices(
    [property: JsonPropertyName("data")] List<Invoice> Data,
    [property: JsonPropertyName("meta")] PaginationMeta? Meta = null
) : IPaginatedResponse<Invoice>;

public record InvoiceDetail(
    [property: JsonPropertyName("data")] Invoice? Data
);

public record Invoice(
    [property: JsonPropertyName("id")]                      int            Id,
    [property: JsonPropertyName("invoice_date")]            string?        InvoiceDate,
    [property: JsonPropertyName("invoice_number")]          string?        InvoiceNumber,
    [property: JsonPropertyName("due_date")]                string?        DueDate,
    [property: JsonPropertyName("reference_number")]        string?        ReferenceNumber,
    [property: JsonPropertyName("status")]                  string         Status,
    [property: JsonPropertyName("paid_status")]             string?        PaidStatus,
    [property: JsonPropertyName("notes")]                   string?        Notes,
    [property: JsonPropertyName("discount_type")]           string?        DiscountType,
    [property: JsonPropertyName("discount")]                decimal        Discount,
    [property: JsonPropertyName("discount_val")]            decimal        DiscountValue,
    [property: JsonPropertyName("sub_total")]               decimal        SubTotal,
    [property: JsonPropertyName("total")]                   decimal        Total,
    [property: JsonPropertyName("tax")]                     decimal        Tax,
    [property: JsonPropertyName("due_amount")]              decimal        DueAmount,
    [property: JsonPropertyName("sent")]                    int            Sent,
    [property: JsonPropertyName("viewed")]                  int            Viewed,
    [property: JsonPropertyName("sequence_number")]         int            SequenceNumber,
    // Nullable : peut être renvoyé null par l'API (aucune conversion appliquée).
    [property: JsonPropertyName("exchange_rate")]           decimal?       ExchangeRate,
    [property: JsonPropertyName("base_sub_total")]          decimal        BaseSubTotal,
    [property: JsonPropertyName("base_total")]              decimal        BaseTotal,
    [property: JsonPropertyName("base_tax")]                decimal        BaseTax,
    [property: JsonPropertyName("base_due_amount")]         decimal        BaseDueAmount,
    [property: JsonPropertyName("base_discount_val")]       decimal        BaseDiscountValue,
    [property: JsonPropertyName("customer_id")]             int            CustomerId,
    [property: JsonPropertyName("currency_id")]             int?           CurrencyId,
    [property: JsonPropertyName("creator_id")]              int?           CreatorId,
    [property: JsonPropertyName("recurring_invoice_id")]    int?           RecurringInvoiceId,
    [property: JsonPropertyName("unique_hash")]             string?        UniqueHash,
    [property: JsonPropertyName("template_name")]           string?        TemplateName,
    [property: JsonPropertyName("invoice_pdf_url")]         string?        InvoicePdfUrl,
    [property: JsonPropertyName("formatted_invoice_date")]  string?        FormattedInvoiceDate,
    [property: JsonPropertyName("formatted_due_date")]      string?        FormattedDueDate,
    [property: JsonPropertyName("formatted_created_at")]    string?        FormattedCreatedAt,
    [property: JsonPropertyName("allow_edit")]              bool           AllowEdit,
    [property: JsonPropertyName("payment_module_enabled")]  bool           PaymentModuleEnabled,
    [property: JsonPropertyName("overdue")]                 int            Overdue,
    [property: JsonPropertyName("sales_tax_type")]          string?        SalesTaxType,
    [property: JsonPropertyName("items")]                   List<Item>?    Items,
    [property: JsonPropertyName("customer")]                Customer?      Customer,
    [property: JsonPropertyName("creator")]                 Creator?       Creator,
    [property: JsonPropertyName("fields")]                  List<Field>?   Fields,
    [property: JsonPropertyName("company")]                 Company?       Company,
    [property: JsonPropertyName("currency")]                Currency?      Currency
)
{
    // ── Statut ────────────────────────────────────────────────────────────

    // Le champ "status" renvoyé par l'API (DRAFT/SENT/COMPLETED) n'est pas
    // toujours réévalué en temps réel côté serveur : une facture dont la
    // due_date est dépassée peut donc encore arriver avec status="SENT"
    // (et overdue=0) tant que le job de recalcul du serveur n'est pas repassé.
    // On recalcule donc le retard nous-mêmes à partir de la date d'échéance
    // et du solde restant dû, sans se fier uniquement au statut/flag serveur.
    public bool IsOverdue =>
        Status != "COMPLETED"
        && DueAmount > 0
        && DueDate is not null
        && DateTime.TryParse(DueDate, out DateTime due)
        && due.Date < DateTime.Today;

    public string FormattedStatus => Status switch
    {
        _ when IsOverdue => AppStrings.Get("Status_Overdue"),
        "DRAFT"          => AppStrings.Get("Status_Draft"),
        "SENT"           => AppStrings.Get("Status_InvoiceSent"),
        "COMPLETED"      => AppStrings.Get("Status_Completed"),
        "OVERDUE"        => AppStrings.Get("Status_Overdue"),
        _                => Status
    };

    public Color StatusColor => Status switch
    {
        _ when IsOverdue => Color.FromArgb("#EF4444"),
        "DRAFT"          => Color.FromArgb("#94A3B8"),
        "SENT"           => Color.FromArgb("#F59E0B"),
        "COMPLETED"      => Color.FromArgb("#10B981"),
        "OVERDUE"        => Color.FromArgb("#EF4444"),
        _                => Color.FromArgb("#64748B")
    };

    // ── Montants formatés ─────────────────────────────────────────────────

    public string FormattedTotal     => FormatAmount(Total);
    public string FormattedSubTotal  => FormatAmount(SubTotal);
    public string FormattedTax       => FormatAmount(Tax);
    public string FormattedDueAmount => FormatAmount(DueAmount);
    public string FormattedDiscount  => DiscountValue == 0 ? "–" : $"-{FormatAmount(DiscountValue)}";

    public bool HasDueAmount => DueAmount > 0;

    // ── Progression jusqu'à l'échéance ────────────────────────────────────
    // Fraction (0 à 1) du temps écoulé entre la date de facture et la date
    // d'échéance, utilisée pour afficher une barre de progression visuelle.

    public bool ShowDueProgress =>
        DateTime.TryParse(InvoiceDate, out _) && DateTime.TryParse(DueDate, out _);

    public double DueProgress
    {
        get
        {
            if (!DateTime.TryParse(InvoiceDate, out DateTime invoiceDate)
                || !DateTime.TryParse(DueDate, out DateTime dueDate))
            {
                return 0d;
            }

            double totalDays = (dueDate.Date - invoiceDate.Date).TotalDays;
            if (totalDays <= 0)
            {
                return 1d;
            }

            double elapsedDays = (DateTime.Today - invoiceDate.Date).TotalDays;
            double fraction = elapsedDays / totalDays;
            return Math.Clamp(fraction, 0d, 1d);
        }
    }

    // Vert de 0 à 33%, orange de 33 à 66%, rouge de 66 à 100%.
    public Color DueProgressColor
    {
        get
        {
            double progress = DueProgress;
            if (progress <= 0.33d)
            {
                return Color.FromArgb("#10B981");
            }

            if (progress <= 0.66d)
            {
                return Color.FromArgb("#F59E0B");
            }

            return Color.FromArgb("#EF4444");
        }
    }

    public string DueProgressLabel
    {
        get
        {
            if (!DateTime.TryParse(DueDate, out DateTime dueDate))
            {
                return string.Empty;
            }

            int daysRemaining = (dueDate.Date - DateTime.Today).Days;
            return daysRemaining switch
            {
                > 0  => string.Format(AppStrings.Get("Invoice_DaysRemainingFormat"), daysRemaining),
                0    => AppStrings.Get("Invoice_DueTodayLabel"),
                _    => string.Format(AppStrings.Get("Invoice_OverdueDaysFormat"), Math.Abs(daysRemaining))
            };
        }
    }

    private string FormatAmount(decimal centimes)
    {
        decimal amount = centimes / 100m;
        return Currency is not null && !string.IsNullOrEmpty(Currency.Symbol)
            ? $"{Currency.Symbol}{amount:N2}"
            : $"{amount:N2}";
    }
}
