using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AionDpsMeter.UI.Converters
{
    public class PercentageToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return 0.0;

            if (!TryToDouble(values[0], out var percentage) || !TryToDouble(values[1], out var maxWidth))
                return 0.0;

            percentage = Math.Clamp(percentage, 0.0, 100.0);
            maxWidth = Math.Max(0.0, maxWidth);

            return maxWidth * (percentage / 100.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static bool TryToDouble(object? value, out double result)
        {
            result = 0.0;

            if (value is null || value == DependencyProperty.UnsetValue)
                return false;

            if (value is double d)
            {
                result = d;
                return true;
            }

            if (value is IConvertible)
            {
                try
                {
                    result = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
