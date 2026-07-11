using System.Globalization;

namespace InvoiceShelf.Helpers
{
    /// <summary>Inverse un bool, ou renvoie true si la valeur est null/vide.</summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                bool b          => !b,
                string s        => string.IsNullOrWhiteSpace(s),
                null            => true,
                _               => false
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : value!;
    }
}
