using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AionDpsMeter.UI.Views.Layouts
{
    public partial class Style1LayoutControl : UserControl
    {
        public event RoutedEventHandler? HistoryClicked;
        public event RoutedEventHandler? StatEfficiencyCalculatorClicked;
        public event RoutedEventHandler? SettingsClicked;
        public event RoutedEventHandler? MinimizeClicked;
        public event RoutedEventHandler? CloseClicked;
        public event RoutedEventHandler? WhatsNewClicked;
        public event MouseButtonEventHandler? PlayerItemClicked;

        public Style1LayoutControl()
        {
            InitializeComponent();
        }

        public void SetBackgroundImage(string? path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                PlayersBackgroundImage.Source = null;
                PlayersBackgroundOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                PlayersBackgroundImage.Source = bitmap;
                PlayersBackgroundOverlay.Visibility = Visibility.Visible;
            }
            catch
            {
                PlayersBackgroundImage.Source = null;
                PlayersBackgroundOverlay.Visibility = Visibility.Collapsed;
            }
        }

        public void ClearBackgroundImage()
        {
            PlayersBackgroundImage.Source = null;
            PlayersBackgroundOverlay.Visibility = Visibility.Collapsed;
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryClicked?.Invoke(sender, e);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsClicked?.Invoke(sender, e);
        }

        private void StatEfficiencyCalculatorButton_Click(object sender, RoutedEventArgs e)
        {
            StatEfficiencyCalculatorClicked?.Invoke(sender, e);
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
