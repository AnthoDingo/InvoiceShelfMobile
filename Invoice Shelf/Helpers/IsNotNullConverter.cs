using System.Globalization;

namespace InvoiceShelf.Helpers
{
    /// <summary>Renvoie true si la valeur est non-null et non-vide (string).</summary>
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                null            => false,
                string s        => !string.IsNullOrWhiteSpace(s),
                bool b          => b,
                int i           => i != 0,
                _               => true
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
