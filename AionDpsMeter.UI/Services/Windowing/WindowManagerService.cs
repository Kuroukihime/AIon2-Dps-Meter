using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Services.Windowing.Native;
using AionDpsMeter.UI.Utils;

namespace AionDpsMeter.UI.Services.Windowing;

public sealed class WindowManagerService(IAppSettingsService settingsService, GameFocusWatcher focusWatcher)
    : IWindowManagerService
{
    private const double PositionGap = 8;

    private static readonly HashSet<WindowKey> GameAnchoredKeys =
        [WindowKey.Main, WindowKey.TimersOverlay, WindowKey.BuffOverlay, WindowKey.SkillCdOverlay];

    private readonly Dispatcher _uiDispatcher =
        Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

    private readonly Lock _gate = new();

    private readonly Dictionary<WindowSlot, Window> _windows = new();

    private readonly Dictionary<Window, ClickThroughState> _clickThroughStates = new();

    public event EventHandler? CloseAppCommand;

    #region Window lifecycle

    public void CloseApplication() =>
        CloseAppCommand?.Invoke(this, EventArgs.Empty);

    public void Open(
        WindowKey key,
        Window window,
        bool isSingleton,
        string? instanceId = null,
        Window? owner = null)
    {
        RunOnUiThread(() =>
        {
            var slot = WindowSlot.ForNewWindow(key, isSingleton, instanceId);

            if (TryGetWindow(slot, out var existing))
            {
                FocusCore(existing);
                return;
            }

            if (owner is not null)
            {
                window.Owner = owner;
                PositionToRightOf(window, owner);
            }

            lock (_gate)
            {
                _windows[slot] = window;
            }

            window.Closed += (_, _) => OnWindowClosed(slot, window);
            window.Show();

            if (GameAnchoredKeys.Contains(key))
            {
                SnapGuard.Attach(window);
            }
        });
    }

    public void Hide(WindowKey key, string? instanceId = null) =>
        RunOnUiThread(() =>
            WithWindow(key, instanceId, window => window.Hide()));

    public void Minimize(WindowKey key, string? instanceId = null) =>
        RunOnUiThread(() =>
            WithWindow(key, instanceId, window => window.WindowState = WindowState.Minimized));

    public void Close(WindowKey key, string? instanceId = null) =>
        RunOnUiThread(() =>
            WithWindow(key, instanceId, window => window.Close()));

    public void CloseAll() =>
        RunOnUiThread(() =>
        {
            List<Window> snapshot;

            lock (_gate)
            {
                snapshot = _windows.Values.ToList();
            }

            foreach (var window in snapshot)
            {
                window.Close();
            }
        });

    public void Focus(WindowKey key, string? instanceId = null) =>
        RunOnUiThread(() =>
            WithWindow(key, instanceId, FocusCore));

    public void Drag(WindowKey key, string? instanceId = null) =>
        RunOnUiThread(() =>
            WithWindow(key, instanceId, window =>
            {
                // A mouse-down can reach the window late; starting a move after the button was released
                // would leave Windows in move mode waiting for a release that already happened.
                if (!IsPrimaryButtonPressed()) return;

                TryDragMove(window);

                if (GameAnchoredKeys.Contains(key))
                {
                    SaveGameRelativePosition(key, window);
                }
            }));

    public bool IsOpen(WindowKey key, string? instanceId = null) =>
        TryGetWindow(WindowSlot.ForLookup(key, instanceId), out _);

    public Size PlaceOverGame(WindowKey key, Rect gameRect, Func<Size, Point> defaultTopLeft)
    {
        var size = Size.Empty;

        RunOnUiThread(() =>
            WithWindow(key, null, window =>
            {
                size = GetWindowSize(window);

                var target = settingsService.TryGetGameRelativePosition(key, out var relative) && relative is not null
                    ? new Point(gameRect.Left + relative.X * gameRect.Width, gameRect.Top + relative.Y * gameRect.Height)
                    : defaultTopLeft(size);

                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = Clamp(target.X, gameRect.Left, gameRect.Right - size.Width);
                window.Top = Clamp(target.Y, gameRect.Top, gameRect.Bottom - size.Height);
            }));

        return size;
    }

    #endregion

    #region Click-through (public API)

    public void SetClickThrough(WindowKey key, string? instanceId = null) =>
        RunOnUiThread(() =>
            WithWindow(key, instanceId, EnableClickThrough));

    public void RestoreClickThrough(WindowKey key, string? instanceId = null) =>
        RunOnUiThread(() =>
            WithWindow(key, instanceId, DisableClickThrough));

    public bool IsClickThrough(WindowKey key, string? instanceId = null)
    {
        var result = false;

        RunOnUiThread(() =>
            WithWindow(key, instanceId, window => result = IsClickThroughEnabled(window)));

        return result;
    }

    #endregion

    #region Click-through (implementation)

    private bool IsClickThroughEnabled(Window window)
    {
        lock (_gate)
        {
            return _clickThroughStates.TryGetValue(window, out var state) && state.IsEnabled;
        }
    }

    private void EnableClickThrough(Window window)
    {
        if (IsClickThroughEnabled(window))
        {
            return;
        }

        var hwnd = new WindowInteropHelper(window).Handle;

        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var state = new ClickThroughState
        {
            WindowHandle = hwnd,
            WindowExtendedStyle = NativeWindowHelper.GetExtendedStyle(hwnd)
        };

        NativeWindowHelper.SetExtendedStyle(
            hwnd,
            state.WindowExtendedStyle | NativeWindowHelper.ClickThroughExtendedStyle);

        // Act on the children that exist now: WebView2 creates its input windows after startup, so a snapshot goes stale.
        foreach (var child in NativeWindowHelper.GetChildWindows(hwnd))
        {
            NativeWindowHelper.SetEnabled(child, enabled: false);
        }

        lock (_gate)
        {
            _clickThroughStates[window] = state;
            state.IsEnabled = true;
        }
    }

    private void DisableClickThrough(Window window)
    {
        ClickThroughState? state;

        lock (_gate)
        {
            if (!_clickThroughStates.Remove(window, out state))
            {
                return;
            }
        }

        if (!NativeWindowHelper.IsValid(state.WindowHandle))
        {
            return;
        }

        NativeWindowHelper.SetExtendedStyle(state.WindowHandle, state.WindowExtendedStyle);

        // Every child must take input outside click-through, including ones created after click-through was turned on.
        foreach (var child in NativeWindowHelper.GetChildWindows(state.WindowHandle))
        {
            NativeWindowHelper.SetEnabled(child, enabled: true);
        }
    }

    private static bool IsPrimaryButtonPressed()
    {
        // GetAsyncKeyState reports physical buttons, so honor a left-handed button swap.
        var primary = NativeMethods.GetSystemMetrics(NativeMethods.SM_SWAPBUTTON) != 0 ? NativeMethods.VK_RBUTTON : NativeMethods.VK_LBUTTON;
        return (NativeMethods.GetAsyncKeyState(primary) & 0x8000) != 0;
    }

    #endregion

    #region Window tracking

    private bool TryGetWindow(WindowSlot slot, [NotNullWhen(true)] out Window? window)
    {
        lock (_gate)
        {
            if (_windows.TryGetValue(slot, out var tracked))
            {
                window = tracked;
                return true;
            }
        }

        window = null;
        return false;
    }

    private void WithWindow(WindowKey key, string? instanceId, Action<Window> action)
    {
        if (TryGetWindow(WindowSlot.ForLookup(key, instanceId), out var window))
        {
            action(window);
        }
    }

    private void OnWindowClosed(WindowSlot slot, Window window)
    {
        // Always restore native state before forgetting the window.
        DisableClickThrough(window);

        lock (_gate)
        {
            if (_windows.TryGetValue(slot, out var tracked) &&
                ReferenceEquals(tracked, window))
            {
                _windows.Remove(slot);
            }
        }
    }

    #endregion

    #region Window behavior and layout

    private static void FocusCore(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        window.Activate();
    }

    private void TryDragMove(Window window)
    {
        try
        {
            window.DragMove();
        }
        catch (InvalidOperationException)
        {
            // Not in an active left-button-down, or the window isn't in the Normal state.
        }
    }

    private static void PositionToRightOf(Window child, Window owner)
    {
        var workArea = ScreenHelper.GetWorkingAreaForWindow(owner);

        var preferredLeft = owner.Left + owner.Width + PositionGap;
        var fitsOnScreen = preferredLeft + child.Width <= workArea.Right;

        var left = fitsOnScreen
            ? preferredLeft
            : Math.Max(workArea.Left, workArea.Right - child.Width);

        var top = Clamp(owner.Top, workArea.Top, workArea.Bottom - child.Height);

        child.WindowStartupLocation = WindowStartupLocation.Manual;
        child.Left = left;
        child.Top = top;
    }

    private void SaveGameRelativePosition(WindowKey key, Window window)
    {
        var game = focusWatcher.FindGameWindow();
        if (game == IntPtr.Zero || ScreenHelper.GetWindowRectDips(game, window) is not { } gameRect)
        {
            return;
        }

        settingsService.SetGameRelativePosition(
            key,
            new GameRelativePosition
            {
                X = (window.Left - gameRect.Left) / gameRect.Width,
                Y = (window.Top - gameRect.Top) / gameRect.Height
            });
    }

    private static Size GetWindowSize(Window window) =>
        new(window.ActualWidth > 0 ? window.ActualWidth : window.Width,
            window.ActualHeight > 0 ? window.ActualHeight : window.Height);

    private static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(value, max));

    #endregion

    #region Threading

    private void RunOnUiThread(Action action)
    {
        if (_uiDispatcher.CheckAccess())
        {
            action();
            return;
        }

        try
        {
            _uiDispatcher.Invoke(action, DispatcherPriority.Normal);
        }
        catch (TaskCanceledException)
        {
            // The dispatcher is shutting down; there is nothing left to run the action on.
        }
    }

    #endregion

    #region Nested types

    private readonly record struct WindowSlot(WindowKey Key, string InstanceId)
    {
        public static WindowSlot ForLookup(WindowKey key, string? instanceId) =>
            new(key, instanceId ?? string.Empty);

        public static WindowSlot ForNewWindow(WindowKey key, bool isSingleton, string? instanceId) =>
            isSingleton
                ? new WindowSlot(key, string.Empty)
                : new WindowSlot(key, instanceId ?? Guid.NewGuid().ToString("N"));
    }


    #endregion
}
