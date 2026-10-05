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
        private AbyssCorridorSettings corridorSettings = new();

        private string clock = string.Empty;
        private string corridorCountdown = string.Empty;
        private bool isCorridorSoon;

        protected override void OnInitialized()
        {
            LoadSettings();
            Refresh();
            appSettingsService.SettingsChanged += OnSettingsChanged;

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
            clock = nowUtc.ToLocalTime().ToString("HH:mm:ss");

            var nextSpawnUtc = GameDataProvider.Instance.AbyssCorridor.NextOccurrenceUtc(nowUtc);
            if (nextSpawnUtc is null)
            {
                corridorCountdown = string.Empty;
                isCorridorSoon = false;
                return;
            }

            var remaining = nextSpawnUtc.Value - nowUtc;
            corridorCountdown = $"{(int)remaining.TotalHours}:{remaining:mm\\:ss}";
            isCorridorSoon = remaining <= TimeSpan.FromMinutes(corridorSettings.LeadMinutes);
        }

        private void LoadSettings()
        {
            overlaySettings = appSettingsService.TimersOverlaySettings;
            corridorSettings = appSettingsService.AbyssCorridorSettings;
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            LoadSettings();
            InvokeAsync(StateHasChanged);
        }

        private void BeginDrag(MouseEventArgs _)
        {
            if (!IsEditable) return;
            windowManager.Drag(WindowKey.TimersOverlay);
        }

        public void Dispose()
        {
            appSettingsService.SettingsChanged -= OnSettingsChanged;
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;
        }
    }
}
