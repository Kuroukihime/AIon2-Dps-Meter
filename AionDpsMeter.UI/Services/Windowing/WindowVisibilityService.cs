using System.Windows;
using AionDpsMeter.Services.Services.Settings;

namespace AionDpsMeter.UI.Services.Windowing
{
    [Flags]
    public enum HideReason
    {
        None = 0,
        Tray = 1,
        GameNotFocused = 2
    }

    /// <summary>
    /// Hides all app windows while any <see cref="HideReason"/> applies and shows them again once none does.
    /// </summary>
    public sealed class WindowVisibilityService(GameFocusWatcher focusWatcher, IAppSettingsService settingsService)
    {
        private readonly List<Window> _hiddenWindows = new();
        private HideReason _reasons;

        // Set by an explicit tray restore so the app stays reachable without the game; cleared once focus leaves the app.
        private bool _restoredByUser;

        public bool IsHiddenBy(HideReason reason) => (_reasons & reason) != 0;

        public void Start()
        {
            focusWatcher.ForegroundChanged += (_, _) => ApplyGameFocus();
            settingsService.SettingsChanged += (_, _) =>
                Application.Current.Dispatcher.InvokeAsync(ApplyGameFocus);

            focusWatcher.Start();
            ApplyGameFocus();
        }

        public void Hide(HideReason reason)
        {
            if (_reasons == HideReason.None)
            {
                _hiddenWindows.Clear();
                _hiddenWindows.AddRange(Application.Current.Windows.OfType<Window>().Where(w => w.IsVisible));
                foreach (var window in _hiddenWindows)
                    window.Hide();
            }

            _reasons |= reason;
        }

        /// <summary>
        /// Clears one reason. Windows reappear without taking focus, so the game keeps keyboard input.
        /// </summary>
        public void Clear(HideReason reason)
        {
            if (!IsHiddenBy(reason)) return;

            _reasons &= ~reason;
            if (_reasons == HideReason.None)
                ShowHiddenWindows();
        }

        /// <summary>
        /// Explicit user restore: clears every reason and focuses the main window.
        /// </summary>
        public void RestoreAll()
        {
            _restoredByUser = true;
            if (_reasons != HideReason.None)
            {
                _reasons = HideReason.None;
                ShowHiddenWindows();
            }

            Application.Current.MainWindow?.Activate();
        }

        private void ApplyGameFocus()
        {
            var foreground = focusWatcher.Foreground;
            if (foreground == ForegroundKind.Other)
                _restoredByUser = false;

            var visible = !settingsService.ShowOnlyOverGame
                || foreground == ForegroundKind.Game
                || (foreground == ForegroundKind.Self && (_restoredByUser || focusWatcher.IsGameRunning()));

            if (visible)
                Clear(HideReason.GameNotFocused);
            else
                Hide(HideReason.GameNotFocused);
        }

        private void ShowHiddenWindows()
        {
            var openWindows = Application.Current.Windows.OfType<Window>().ToHashSet();
            foreach (var window in _hiddenWindows.Where(openWindows.Contains))
            {
                var showActivated = window.ShowActivated;
                window.ShowActivated = false;
                window.Show();
                window.ShowActivated = showActivated;

                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
            }

            _hiddenWindows.Clear();
        }
    }
}
