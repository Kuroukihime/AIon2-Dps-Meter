using AionDpsMeter.UI.Pages;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using System.Windows;

namespace AionDpsMeter.UI.Views
{
    public partial class TimersOverlayWindow : Window
    {
        public TimersOverlayWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => InitializeTimersWebView();
        }

        private void InitializeTimersWebView()
        {
            TimersWebView.WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            TimersWebView.Services = App.AppHost.Services;
            TimersWebView.RootComponents.Clear();
            TimersWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(TimersOverlay)
            });
        }
    }
}
