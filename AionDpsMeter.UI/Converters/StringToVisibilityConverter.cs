using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AionDpsMeter.UI.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if(int.TryParse(stringValue, out var val))
                {
                    if (val == 0) return Visibility.Collapsed;
                }

                return string.IsNullOrEmpty(stringValue) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
