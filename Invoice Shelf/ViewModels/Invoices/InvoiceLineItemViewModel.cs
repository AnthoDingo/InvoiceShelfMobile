using System.Globalization;
using InvoiceShelf.Models.Admin;

namespace InvoiceShelf.ViewModels.Invoices;

/// <summary>
/// Une ligne d'article éditable dans le formulaire de création de facture.
/// Les montants sont saisis en unités courantes (ex. "12.50") ; la conversion
/// en centimes n'a lieu qu'au moment de construire le payload d'API.
/// </summary>
public partial class InvoiceLineItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(FormattedLineTotal))]
    private string _quantity = "1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(FormattedLineTotal))]
    private string _unitPrice = string.Empty;

    /// <summary>Article du catalogue sélectionné pour pré-remplir cette ligne (optionnel).</summary>
    [ObservableProperty]
    private CatalogItem? _selectedCatalogItem;

    public decimal ParsedQuantity => ParseDecimal(Quantity);
    public decimal ParsedUnitPrice => ParseDecimal(UnitPrice);

    /// <summary>Total de la ligne (quantité × prix unitaire), en unités courantes.</summary>
    public decimal LineTotal => ParsedQuantity * ParsedUnitPrice;

    public string FormattedLineTotal => LineTotal.ToString("N2", CultureInfo.CurrentCulture);

    /// <summary>Vrai si la ligne est exploitable (nom renseigné, quantité et prix valides).</summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && ParsedQuantity > 0 && ParsedUnitPrice >= 0;

    // Choisir un article catalogue pré-remplit nom/description/prix, mais tout
    // reste modifiable ensuite (ex. remise ponctuelle sur le prix affiché).
    partial void OnSelectedCatalogItemChanged(CatalogItem? value)
    {
        if (value is null) return;

        Name = value.Name;
        Description = value.Description;
        UnitPrice = (value.Price / 100m).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static decimal ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        string normalized = s.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            ? value
            : 0m;
    }
}
