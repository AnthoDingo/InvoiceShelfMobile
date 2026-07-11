using System.Globalization;

namespace InvoiceShelf.Helpers
{
    /// <summary>Convertit un montant en centimes (int/decimal) en chaîne formatée.</summary>
    public class CentimesConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            decimal amount = value switch
            {
                decimal d => d / 100m,
                int i     => i / 100m,
                long l    => l / 100m,
                _         => 0m
            };
            return $"{amount:N2}";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
