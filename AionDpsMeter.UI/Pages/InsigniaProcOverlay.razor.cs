using AionDpsMeter.Core.Windowing;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Services.Windowing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text.RegularExpressions;

namespace AionDpsMeter.UI.Pages
{
    partial class InsigniaProcOverlay(IWindowManagerService windowManager, WindowHelper windowHelper, IAppSettingsService settingsService, IPlayerBuffStackTracker buffStackTracker) : IDisposable
    {
        private const int TrackedBuffId = 13000001;
        private const string DefaultColor = "#a814b5";
        private static readonly Regex HexColorRegex = new("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

        private bool isBuffActive;

     
        [Parameter]
        public string Color { get; set; } = DefaultColor;

  
        [Parameter]
        public string? TexturePath { get; set; }

        private static readonly Regex SafeUrlRegex = new(@"^[\w\-./:%]+$", RegexOptions.Compiled);

        private string RootStyle
        {
            get
            {
                var style = $"--proc-color:{SafeColor};";
                if (!string.IsNullOrWhiteSpace(TexturePath) && SafeUrlRegex.IsMatch(TexturePath.Trim()))
                    style += $"--proc-texture:url('{TexturePath.Trim()}');";
                return style;
            }
        }

        private string SafeColor => !string.IsNullOrWhiteSpace(Color) && HexColorRegex.IsMatch(Color.Trim())
            ? Color.Trim()
            : DefaultColor;

        private bool IsEditable => windowHelper.IsInsigniaProcEdit;

        private bool ShouldShowOverlay => IsEditable || isBuffActive;

        protected override void OnInitialized()
        {
            isBuffActive = buffStackTracker.BuffsAtOrAboveThreshold.Any(b => b.BuffId == TrackedBuffId);

            buffStackTracker.BuffActivated += OnBuffActivated;
            buffStackTracker.BuffDeactivated += OnBuffDeactivated;
            windowHelper.WindowStateUpdated += OnWindowStateUpdated;
        }

        private void OnBuffActivated(object? sender, int buffId)
        {
            if (buffId != TrackedBuffId) return;
            isBuffActive = true;
            InvokeAsync(StateHasChanged);
        }

        private void OnBuffDeactivated(object? sender, int buffId)
        {
            if (buffId != TrackedBuffId) return;
            isBuffActive = false;
            InvokeAsync(StateHasChanged);
        }

        private void OnWindowStateUpdated(object? sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        private void BeginDrag(MouseEventArgs _)
        {
            if (!IsEditable) return;
            windowManager.Drag(WindowKey.InsigniaProcOverlay);
        }

        public void Dispose()
        {
            buffStackTracker.BuffActivated -= OnBuffActivated;
            buffStackTracker.BuffDeactivated -= OnBuffDeactivated;
            windowHelper.WindowStateUpdated -= OnWindowStateUpdated;
        }
    }
}