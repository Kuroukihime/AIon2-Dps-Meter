using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class SettingsWindow : Window
    {
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
    }
}
