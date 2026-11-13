using System.Windows;
using AionDpsMeter.UI.Pages;
using AionDpsMeter.UI.Services.Windowing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebView.Wpf;

namespace AionDpsMeter.UI.Views
{
    public partial class InsigniaProcOverlayWindow : Window
    {
        private WindowHelper? windowHelper;

        public InsigniaProcOverlayWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => InitializeProcWebView();
            Loaded += (_, _) => InitializeWindowStateSync();
            Closed += (_, _) => DisposeWindowStateSync();
        }

        private void InitializeProcWebView()
        {
            ProcWebView.WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            ProcWebView.Services = App.AppHost.Services;
            ProcWebView.RootComponents.Clear();
            ProcWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(InsigniaProcOverlay)
            });
        }

        private void InitializeWindowStateSync()
        {
            windowHelper = App.AppHost.Services.GetRequiredService<WindowHelper>();
            windowHelper.WindowStateUpdated += OnWindowStateUpdated;
            ApplyResizeMode();
        }

        private void DisposeWindowStateSync()
        {
            if (windowHelper is null) return;
            windowHelper.WindowStateUpdated -= OnWindowStateUpdated;
            windowHelper = null;
        }

        private void OnWindowStateUpdated(object? sender, EventArgs e)
        {
            ApplyResizeMode();
        }

        private void ApplyResizeMode()
        {
            ResizeMode = windowHelper?.IsInsigniaProcEdit == true
                ? ResizeMode.CanResizeWithGrip
                : ResizeMode.NoResize;
        }
    }
}
