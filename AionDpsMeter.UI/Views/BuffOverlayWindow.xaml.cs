using AionDpsMeter.UI.Pages;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using System.Windows;

namespace AionDpsMeter.UI.Views
{
    /// <summary>
    /// Interaction logic for BuffOverlay.xaml
    /// </summary>
    public partial class BuffOverlayWindow : Window
    {
        public BuffOverlayWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => InitializeBuffWebView();
        }

        private void InitializeBuffWebView()
        {
            BuffWebView.WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            BuffWebView.Services = App.AppHost.Services;
            BuffWebView.RootComponents.Clear();
            BuffWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(BuffOverlay)
            });
        }

    }
}
