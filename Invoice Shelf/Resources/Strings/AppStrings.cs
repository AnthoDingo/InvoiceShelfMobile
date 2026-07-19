using System.Globalization;
using System.Resources;

namespace InvoiceShelf.Resources.Strings
{
    /// <summary>
    /// Accès centralisé aux chaînes localisées (FR/EN) définies dans
    /// AppStrings.resx (anglais, culture neutre / fallback) et AppStrings.fr.resx (français).
    /// La langue est déterminée automatiquement par la culture du système
    /// (CultureInfo.CurrentUICulture) : aucun choix de langue n'est proposé à l'utilisateur.
    /// </summary>
    public static class AppStrings
    {
        private static readonly ResourceManager ResourceManagerInstance =
            new ResourceManager("InvoiceShelf.Resources.Strings.AppStrings", typeof(AppStrings).Assembly);

        public static ResourceManager ResourceManager => ResourceManagerInstance;

        /// <summary>
        /// Retourne la chaîne localisée pour la clé donnée, dans la langue courante du système.
        /// Si la clé est introuvable, retourne la clé elle-même (visible en dev, jamais silencieux).
        /// </summary>
        public static string Get(string key)
        {
            string? value = ResourceManagerInstance.GetString(key, CultureInfo.CurrentUICulture);
            return value ?? key;
        }
    }
}
