using System.Windows;
using System.Windows.Threading;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Views;

namespace AionDpsMeter.UI.Services.Windowing
{
    /// <summary>
    /// With "Show Meter Only in Combat" on, hides the meter window outside combat. It never acts while
    /// <see cref="WindowVisibilityService"/> is hiding the app, which owns the windows then.
    /// </summary>
    public sealed class CombatVisibilityService(
        WindowVisibilityService visibility,
        WindowHelper windowHelper,
        CombatSessionManager sessionManager,
        IAppSettingsService settingsService,
        MainWindow meter) : IDisposable
    {
        private static readonly TimeSpan CombatWindow = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMilliseconds(500);

        private DispatcherTimer? _timer;
        private bool _hiddenOutOfCombat;

        public void Start()
        {
            _timer = new DispatcherTimer { Interval = CheckInterval };
            _timer.Tick += (_, _) => Apply();
            _timer.Start();
        }

        public void Dispose() => _timer?.Stop();

        private void Apply()
        {
            bool inCombat = sessionManager.IsInCombat(CombatWindow);
            // Holding the move key or having Settings open shows the meter so it can be placed or configured out of combat.
            bool wanted = !settingsService.ShowMeterOnlyInCombat || inCombat || windowHelper.IsMoveKeyHeld || windowHelper.IsSettingsOpen;

            if (visibility.IsHidden) return;

            if (!wanted && meter.IsVisible)
            {
                meter.Hide();
                _hiddenOutOfCombat = true;
            }
            else if (wanted && _hiddenOutOfCombat)
            {
                var showActivated = meter.ShowActivated;
                meter.ShowActivated = false;
                meter.Show();
                meter.ShowActivated = showActivated;
                _hiddenOutOfCombat = false;
            }
        }
    }
}
