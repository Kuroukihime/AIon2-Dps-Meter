using System.Windows;
using AionDpsMeter.UI.Pages;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using AionDpsMeter.UI.Services.Windowing;

namespace AionDpsMeter.UI.Views
{
    /// <summary>
    /// Interaction logic for SkillCdOverlayWindow.xaml
    /// </summary>
    public partial class SkillCdOverlayWindow : Window
    {
        public SkillCdOverlayWindow()
        {
            InitializeComponent();
            WebViewEnvironment.Configure(SkillWebView);
            Loaded += (_, _) => InitializeSkillWebView();
        }



        private void InitializeSkillWebView()
        {
            SkillWebView.WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            SkillWebView.Services = App.AppHost.Services;
            SkillWebView.RootComponents.Clear();
            SkillWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(SkillCdOverlay)
            });
        }
    }
}
