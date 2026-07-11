using System.Globalization;

namespace InvoiceShelf.Helpers
{
    public class ShortDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
                return FormatDate(date, culture);

            if (value is string str && DateTime.TryParse(str, out var parsedDate))
                return FormatDate(parsedDate, culture);

            return value;
        }

        private static string FormatDate(DateTime date, CultureInfo culture)
        {
            var monthName = culture.DateTimeFormat.GetMonthName(date.Month);
            // Truncation uniforme : 4 chars max, avec point si le nom est plus long
            if (monthName.Length > 4)
                monthName = $"{monthName.Substring(0, 3)}.";

            return $"{date.Day:00} {monthName} {date:yyyy}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
