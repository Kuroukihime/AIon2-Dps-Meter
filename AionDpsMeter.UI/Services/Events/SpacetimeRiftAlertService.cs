using System.Media;
using System.Windows.Threading;
using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Services.Tray;

namespace AionDpsMeter.UI.Services.Events
{
    /// <summary>
    /// Fires the tray/sound alert once per Spacetime Rift spawn, independent of window visibility.
    /// </summary>
    public sealed class SpacetimeRiftAlertService(IAppSettingsService settingsService, TrayService trayService) : IDisposable
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private DateTime? _alertedSpawnUtc;

        public void Start()
        {
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var nowUtc = DateTime.UtcNow;
            var nextSpawnUtc = GameDataProvider.Instance.SpacetimeRift.NextOccurrenceUtc(nowUtc);
            if (nextSpawnUtc is null || nextSpawnUtc == _alertedSpawnUtc) return;

            var settings = settingsService.SpacetimeRiftSettings;
            var remaining = nextSpawnUtc.Value - nowUtc;
            if (remaining > TimeSpan.FromMinutes(settings.LeadMinutes)) return;

            _alertedSpawnUtc = nextSpawnUtc;

            if (settings.TrayNotification)
            {
                var minutes = (int)Math.Ceiling(remaining.TotalMinutes);
                trayService.ShowNotification("Spacetime Rift", $"Opens in {minutes} min ({nextSpawnUtc.Value.ToLocalTime():HH:mm})");
            }

            if (settings.Sound)
                SystemSounds.Exclamation.Play();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
        }
    }
}
