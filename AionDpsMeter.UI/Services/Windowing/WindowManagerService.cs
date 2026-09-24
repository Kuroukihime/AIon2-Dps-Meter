using AionDpsMeter.Core.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Utils;
using System.Windows;
using System.Windows.Threading;

namespace AionDpsMeter.UI.Services.Windowing
{
    public sealed class WindowManagerService(IAppSettingsService settingsService) : IWindowManagerService
    {

        public event EventHandler? CloseAppCommand;

        private const double PositionGap = 8;

        private readonly Dispatcher uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        private readonly Lock gate = new();
        private readonly Dictionary<(WindowKey Key, string InstanceId), Window> open = new();


        public void CloseApplication() => CloseAppCommand?.Invoke(this, EventArgs.Empty);

        public void Open(WindowKey key, Window window, bool isSingleton, string? instanceId = null, Window? owner = null, WindowPersistenceMode persistenceMode = WindowPersistenceMode.None)
        {
            RunOnUiThread(() =>
            {
                var slot = Slot(key, isSingleton, instanceId);

                Window? existing;
                lock (gate) { open.TryGetValue(slot, out existing); }

                if (existing is not null)
                {
                    FocusCore(existing);
                    return;
                }

                if (owner is not null)
                {
                    window.Owner = owner;
                    PositionToRightOf(window, owner);
                }

                TryRestoreWindowBounds(window, key, persistenceMode);

                lock (gate) { open[slot] = window; }

                window.Closed += (_, _) =>
                {
                    lock (gate)
                    {
                        if (open.TryGetValue(slot, out var tracked) && ReferenceEquals(tracked, window))
                            open.Remove(slot);
                    }
                };

                window.Show();
            });
        }

        public void Hide(WindowKey key, string? instanceId = null) =>
            RunOnUiThread(() => WithWindow(key, instanceId, w => w.Hide()));

        public void Minimize(WindowKey key, string? instanceId = null) =>
            RunOnUiThread(() => WithWindow(key, instanceId, w => w.WindowState = WindowState.Minimized));

        public void Close(WindowKey key, string? instanceId = null) =>
            RunOnUiThread(() => WithWindow(key, instanceId, w => w.Close())); // Closed handler untracks it

        public void CloseAll() =>
            RunOnUiThread(() =>
            {
                List<Window> snapshot;
                lock (gate) { snapshot = open.Values.ToList(); }
                foreach (var w in snapshot) w.Close();
            });

        public void Focus(WindowKey key, string? instanceId = null) =>
            RunOnUiThread(() => WithWindow(key, instanceId, FocusCore));

        public void Drag(WindowKey key, string? instanceId = null) =>
            RunOnUiThread(() => WithWindow(key, instanceId, w =>
            {
                try { w.DragMove(); }
                catch (InvalidOperationException)
                {
                    // Not in an active left-button-down, or window isn't in Normal
                    // state. Harmless — just ignore.
                }

                SaveWindowBounds(key, w);
            }));

        public bool IsOpen(WindowKey key, string? instanceId = null)
        {
            lock (gate)
            {
                // Singleton windows are stored under InstanceId == ""
                return open.ContainsKey((key, instanceId ?? string.Empty));
            }
        }

        // ---------------------------------------------------------------

        private void WithWindow(WindowKey key, string? instanceId, Action<Window> action)
        {
            Window? window;
            lock (gate)
            {
                // instanceId absent -> this must be a singleton window, stored under "".
                var lookupId = instanceId ?? string.Empty;
                window = open.TryGetValue((key, lookupId), out var w) ? w : null;
            }
            if (window is not null) action(window);
        }

        private static (WindowKey, string) Slot(WindowKey key, bool isSingleton, string? instanceId) =>
            isSingleton ? (key, string.Empty) : (key, instanceId ?? Guid.NewGuid().ToString("N"));

        private static void FocusCore(Window w)
        {
            if (w.WindowState == WindowState.Minimized)
                w.WindowState = WindowState.Normal;
            if (!w.IsVisible)
                w.Show();
            w.Activate();
        }

        private static void PositionToRightOf(Window child, Window owner)
        {
            var workArea = ScreenHelper.GetWorkingAreaForWindow(owner);

            double ownerRight = owner.Left + owner.Width;
            double candidateLeft = ownerRight + PositionGap;

            double left = candidateLeft + child.Width <= workArea.Right
                ? candidateLeft
                : Math.Max(workArea.Left, workArea.Right - child.Width);

            double top = Math.Max(workArea.Top, Math.Min(owner.Top, workArea.Bottom - child.Height));

            child.WindowStartupLocation = WindowStartupLocation.Manual;
            child.Left = left;
            child.Top = top;
        }

        private void TryRestoreWindowBounds(Window window, WindowKey key, WindowPersistenceMode persistenceMode)
        {
            if (persistenceMode == WindowPersistenceMode.None)
                return;

            if (!settingsService.TryGetWindowBounds(key, out var saved) || saved is null)
                return;

            var workArea = ScreenHelper.GetWorkingAreaForPoint(saved.Left, saved.Top);

            double width = persistenceMode == WindowPersistenceMode.Bounds
                ? Math.Max(window.MinWidth, saved.Width)
                : window.Width;

            double height = persistenceMode == WindowPersistenceMode.Bounds
                ? Math.Max(window.MinHeight, saved.Height)
                : window.Height;

            if (persistenceMode == WindowPersistenceMode.Bounds)
            {
                window.Width = width;
                window.Height = height;
            }

            double left = Math.Max(workArea.Left, Math.Min(saved.Left, workArea.Right - width));
            double top = Math.Max(workArea.Top, Math.Min(saved.Top, workArea.Bottom - height));

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = left;
            window.Top = top;
        }

        private void SaveWindowBounds(WindowKey key, Window window)
        {
            if (window.WindowState != WindowState.Normal)
                return;

            settingsService.SetWindowBounds(key, new WindowBounds
            {
                Left = window.Left,
                Top = window.Top,
                Width = window.Width,
                Height = window.Height
            });
        }

        private void RunOnUiThread(Action action)
        {
            if (uiDispatcher.CheckAccess())
            {
                action();
                return;
            }

            try
            {
                uiDispatcher.Invoke(action, DispatcherPriority.Normal);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
}
