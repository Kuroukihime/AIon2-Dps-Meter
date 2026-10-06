using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Web.WebView2.Core;

namespace AionDpsMeter.UI.Services.Windowing
{
    /// <summary>
    /// Shared WebView2 environment options. Every BlazorWebView in the process shares one browser instance,
    /// so they must all be created with identical options.
    /// </summary>
    public static class WebViewEnvironment
    {
        // Chromium's occlusion tracker misreads layered, click-through, topmost windows over a fullscreen game
        // as hidden and pauses painting, delaying redraws after Ctrl edit mode or alt-tab.
        private const string BrowserArguments = "--disable-features=CalculateNativeWinOcclusion";

        public static void Configure(BlazorWebView webView) =>
            webView.BlazorWebViewInitializing += (_, e) =>
                e.EnvironmentOptions = new CoreWebView2EnvironmentOptions(additionalBrowserArguments: BrowserArguments);
    }
}
