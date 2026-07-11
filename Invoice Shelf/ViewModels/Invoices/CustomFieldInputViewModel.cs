using System.Globalization;
using InvoiceShelf.Models.Admin;

namespace InvoiceShelf.ViewModels.Invoices;

/// <summary>
/// Une entrée éditable pour un champ personnalisé (défini côté serveur, ex.
/// Réglages &gt; Champs personnalisés d'InvoiceShelf) dans le formulaire de
/// création de facture. Le contrôle affiché (Entry, Editor, Picker, Switch,
/// DatePicker, TimePicker) dépend de Definition.Type ; voir
/// getCustomFieldValueKey côté API pour la correspondance type -> colonne de
/// valeur (string_answer/number_answer/boolean_answer/date_answer/...).
/// </summary>
public partial class CustomFieldInputViewModel : ObservableObject
{
    public CustomField Definition { get; }

    public CustomFieldInputViewModel(CustomField definition)
    {
        Definition = definition;

        // Pré-remplit avec la réponse par défaut configurée côté serveur pour ce
        // champ (ex. valeur par défaut d'un champ "Number" ou "Input").
        if (!string.IsNullOrEmpty(definition.DefaultAnswer))
        {
            _textValue = definition.DefaultAnswer;

            if (bool.TryParse(definition.DefaultAnswer, out bool boolAnswer))
                _boolValue = boolAnswer;

            if (DateTime.TryParse(definition.DefaultAnswer, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateAnswer))
            {
                _dateValue = dateAnswer.Date;
                _timeValue = dateAnswer.TimeOfDay;
            }
        }
    }

    public string Label => Definition.Label ?? Definition.Name;
    public string DisplayLabel => IsRequired ? $"{Label} *" : Label;
    public string? Placeholder => Definition.Placeholder;
    public bool IsRequired => Definition.IsRequired == 1;
    public List<string> Options => Definition.Options ?? [];

    private string Type => Definition.Type ?? "Input";

    // ── Sélecteurs de type (pour l'affichage conditionnel du bon contrôle) ──
    public bool IsSingleLineType => Type is "Input" or "Url" or "Phone";
    public bool IsNumberType     => Type == "Number";
    public bool IsTextAreaType   => Type == "TextArea";
    public bool IsDropdownType   => Type == "Dropdown";
    public bool IsSwitchType     => Type == "Switch";
    public bool IsDateType       => Type == "Date";
    public bool IsTimeType       => Type == "Time";
    public bool IsDateTimeType   => Type == "DateTime";

    [ObservableProperty]
    private string? _textValue;

    [ObservableProperty]
    private bool _boolValue;

    [ObservableProperty]
    private DateTime _dateValue = DateTime.Today;

    [ObservableProperty]
    private TimeSpan _timeValue = DateTime.Now.TimeOfDay;

    /// <summary>Faux uniquement si ce champ est obligatoire et n'a pas de valeur saisie.</summary>
    public bool IsValid
    {
        get
        {
            if (!IsRequired) return true;

            // Switch/Date/Time/DateTime ont toujours une valeur (booléen ou date par
            // défaut) : seuls les champs texte/nombre/liste peuvent être "vides".
            return Type switch
            {
                "Switch" or "Date" or "Time" or "DateTime" => true,
                _ => !string.IsNullOrWhiteSpace(TextValue)
            };
        }
    }

    /// <summary>
    /// Construit la valeur à transmettre au serveur pour ce champ (format attendu
    /// par HasCustomFieldsTrait::addCustomFields selon le type), ou null si le
    /// champ est optionnel et vide.
    /// </summary>
    public object? BuildValue() => Type switch
    {
        "Switch"   => BoolValue,
        "Number"   => decimal.TryParse(NormalizeDecimal(TextValue), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
                        ? number
                        : null,
        "Date"     => DateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        "Time"     => DateTime.Today.Add(TimeValue).ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        "DateTime" => DateValue.Date.Add(TimeValue).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        _          => string.IsNullOrWhiteSpace(TextValue) ? null : TextValue.Trim()
    };

    private static string? NormalizeDecimal(string? s) => s?.Trim().Replace(',', '.');
}
