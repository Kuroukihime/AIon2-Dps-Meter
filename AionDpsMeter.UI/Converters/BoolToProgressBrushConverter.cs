using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AionDpsMeter.UI.Converters
{
    /// <summary>
    /// Returns the user progress-bar gradient brush when the value is <c>true</c>,
    /// otherwise returns the default green gradient brush.
    /// </summary>
    public sealed class BoolToProgressBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isUser = value is true;
            string key = isUser ? "ProgressBarUserGradientBrush" : "ProgressBarGradientBrush";

            if (Application.Current.Resources[key] is Brush brush)
                return brush;

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Style 2 progress bar brush.
    /// IsUser == true  → purple gradient (player's own bar)
    /// IsUser == false → green  gradient (other players)
    /// MultiBinding: [0] bool IsUser, [1] double ActualWidth (unused but forces re-eval on resize)
    /// </summary>
    public sealed class BoolToStyle2ProgressBrushConverter : IMultiValueConverter
    {
        // Player (self) — purple
        private static readonly Color PurpleStart = Color.FromArgb(0xFF, 0x6A, 0x1A, 0x9A);
        private static readonly Color PurpleEnd = Color.FromArgb(0xFF, 0xCE, 0x93, 0xD8);

        // Others — green (matches existing window DPS green)
        private static readonly Color GreenStart = Color.FromArgb(0xFF, 0x1B, 0x5E, 0x20);
        private static readonly Color GreenEnd = Color.FromArgb(0xFF, 0x66, 0xBB, 0x6A);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isUser = values.Length > 0 && values[0] is bool b && b;

            return new LinearGradientBrush(
                isUser ? PurpleStart : GreenStart,
                isUser ? PurpleEnd : GreenEnd,
                new Point(0, 0),
                new Point(1, 0));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
