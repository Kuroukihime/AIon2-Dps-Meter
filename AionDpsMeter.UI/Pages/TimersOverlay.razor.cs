using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Services.Windowing;
using Microsoft.AspNetCore.Components.Web;

namespace AionDpsMeter.UI.Pages
{
    partial class TimersOverlay(IWindowManagerService windowManager, WindowHelper windowHelper, IAppSettingsService appSettingsService)
    {
        private bool IsEditable => windowHelper.IsTimersEdit;
        private CancellationTokenSource? timerCts;
        private TimersOverlaySettings overlaySettings = new();
        private SpacetimeRiftSettings riftSettings = new();

        private string clock = string.Empty;
        private string riftCountdown = string.Empty;
        private bool isRiftSoon;

        protected override void OnInitialized()
        {
            LoadSettings();
            Refresh();
            appSettingsService.SettingsChanged += OnSettingsChanged;
            windowHelper.WindowStateUpdated += OnEditModeChanged;

            timerCts = new CancellationTokenSource();
            _ = RunRefreshTimerAsync(timerCts.Token);
        }

        private async Task RunRefreshTimerAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    await InvokeAsync(() =>
                    {
                        Refresh();
                        StateHasChanged();
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void Refresh()
        {
            var nowUtc = DateTime.UtcNow;
            clock = nowUtc.ToLocalTime().ToString(overlaySettings.Use24HourClock ? "HH:mm:ss" : "h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);

            var nextSpawnUtc = GameDataProvider.Instance.SpacetimeRift.NextOccurrenceUtc(nowUtc);
            if (nextSpawnUtc is null)
            {
                riftCountdown = string.Empty;
                isRiftSoon = false;
                return;
            }

            var remaining = nextSpawnUtc.Value - nowUtc;
            riftCountdown = $"{(int)remaining.TotalHours}:{remaining:mm\\:ss}";
            isRiftSoon = remaining <= TimeSpan.FromMinutes(riftSettings.LeadMinutes);
        }

        private void LoadSettings()
        {
            overlaySettings = appSettingsService.TimersOverlaySettings;
            riftSettings = appSettingsService.SpacetimeRiftSettings;
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            LoadSettings();
            InvokeAsync(StateHasChanged);
        }

        private void OnEditModeChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

        private void BeginDrag(MouseEventArgs _)
        {
            if (!IsEditable) return;
            windowManager.Drag(WindowKey.TimersOverlay);
        }

        public void Dispose()
        {
            appSettingsService.SettingsChanged -= OnSettingsChanged;
            windowHelper.WindowStateUpdated -= OnEditModeChanged;
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;
        }
    }
}
