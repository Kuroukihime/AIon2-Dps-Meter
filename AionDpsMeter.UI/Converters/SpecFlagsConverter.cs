using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AionDpsMeter.UI.Converters
{
    /// <summary>
    /// Converts a bool[] (SpecializationFlags) into a horizontal row of small colored squares.
    /// Active flag (true)  ? bright green  (#4CAF50)
    /// Inactive flag (false) ? dim gray   (#2A3040)
    /// </summary>
    public sealed class SpecFlagsConverter : IValueConverter
    {
        private static readonly Brush ActiveBrush   = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly Brush InactiveBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x30, 0x40));

        static SpecFlagsConverter()
        {
            ActiveBrush.Freeze();
            InactiveBrush.Freeze();
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool[] flags || flags.Length == 0)
                return DependencyProperty.UnsetValue;

            var panel = new StackPanel
            {
                Orientation  = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            foreach (var flag in flags)
            {
                panel.Children.Add(new Rectangle
                {
                    Width          = 7,
                    Height         = 7,
                    RadiusX        = 1,
                    RadiusY        = 1,
                    Fill           = flag ? ActiveBrush : InactiveBrush,
                    Margin         = new Thickness(0, 0, 2, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            return panel;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}
