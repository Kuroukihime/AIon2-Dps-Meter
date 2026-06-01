using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AionDpsMeter.UI
{
    public partial class SettingsWindow : Window
    {
        private static readonly Brush _normalBorder = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D));
        private static readonly Brush _activeBorder = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB));

        public SettingsWindow()
        {
            InitializeComponent();

            var ver = Assembly.GetEntryAssembly()?.GetName().Version;
            VersionTextBlock.Text = ver is not null
                ? $"v{ver.Major}.{ver.Minor}.{ver.Build}"
                : string.Empty;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ThresholdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsDigitsOnly(e.Text);
        }

        private static bool IsDigitsOnly(string text) => Regex.IsMatch(text, @"^\d+$");

        private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.BorderBrush = _activeBorder;
                tb.Text = "Press a key...";
            }
        }

        private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.BorderBrush = _normalBorder;
        }

        private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore standalone modifier keys
            if (key is Key.LeftShift or Key.RightShift or
                       Key.LeftCtrl or Key.RightCtrl or
                       Key.LeftAlt or Key.RightAlt or
                       Key.LWin or Key.RWin or Key.Tab or Key.Escape)
                return;

            var parts = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) parts.Add("Shift");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) parts.Add("Alt");
            parts.Add(key.ToString());

            var combo = string.Join("+", parts);

            if (sender is TextBox tb && DataContext is ViewModels.SettingsViewModel vm)
            {
                vm.ToggleVisibilityHotkey = combo;
                tb.Text = combo;
                Keyboard.ClearFocus();
            }
        }
    }
}

