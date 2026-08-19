using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AionDpsMeter.UI.Views.Layouts
{
    public partial class Style2LayoutControl : UserControl
    {
        public event RoutedEventHandler? HistoryClicked;
        public event RoutedEventHandler? SettingsClicked;
        public event RoutedEventHandler? MinimizeClicked;
        public event RoutedEventHandler? CloseClicked;
        public event RoutedEventHandler? WhatsNewClicked;
        public event MouseButtonEventHandler? PlayerItemClicked;

        public Style2LayoutControl()
        {
            InitializeComponent();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryClicked?.Invoke(sender, e);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsClicked?.Invoke(sender, e);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeClicked?.Invoke(sender, e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(sender, e);
        }

        private void WhatsNewButton_Click(object sender, RoutedEventArgs e)
        {
            WhatsNewClicked?.Invoke(sender, e);
        }

        private void PlayerItem_Click(object sender, MouseButtonEventArgs e)
        {
            PlayerItemClicked?.Invoke(sender, e);
        }
    }
}
