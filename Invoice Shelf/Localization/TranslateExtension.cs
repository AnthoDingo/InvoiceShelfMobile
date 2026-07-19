using System.Globalization;
using InvoiceShelf.Resources.Strings;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace InvoiceShelf.Localization
{
    /// <summary>
    /// Markup extension XAML pour la localisation FR/EN : {loc:Translate Key=MaCle}
    /// La langue affichée dépend uniquement de la culture système au démarrage
    /// de l'application (aucun sélecteur de langue dans l'UI).
    /// </summary>
    [ContentProperty(nameof(Key))]
    public class TranslateExtension : IMarkupExtension<string>
    {
        public string Key { get; set; } = string.Empty;

        public string ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return string.Empty;

            string? value = AppStrings.ResourceManager.GetString(Key, CultureInfo.CurrentUICulture);
            return value ?? Key;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ((IMarkupExtension<string>)this).ProvideValue(serviceProvider);
        }
    }
}
