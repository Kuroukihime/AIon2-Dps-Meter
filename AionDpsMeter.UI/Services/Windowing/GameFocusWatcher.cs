using System.Diagnostics;
using System.Windows.Threading;
using AionDpsMeter.UI.Services.Windowing.Native;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.UI.Services.Windowing
{
    /// <summary>
    /// Tracks whether the foreground window belongs to the game or to this app.
    /// Driven by the foreground WinEvent; a 1 s reconcile corrects any missed event.
    /// </summary>
    public sealed class GameFocusWatcher(ILogger<GameFocusWatcher> logger) : IDisposable
    {
        private const string GameProcessName = "AION2";

        private readonly int _ownProcessId = Environment.ProcessId;

        // Held in a field: the native hook calls this delegate, so it must outlive the hook.
        private NativeMethods.WinEventProc? _callback;
        private IntPtr _hook;
        private readonly DispatcherTimer _reconcileTimer = new() { Interval = TimeSpan.FromSeconds(1) };

        private uint _lastProcessId;
        private bool _lastProcessIsGameOrSelf;

        public bool IsGameOrSelfFocused { get; private set; }

        public event EventHandler? FocusChanged;

        public void Start()
        {
            if (_hook != IntPtr.Zero) return;

            IsGameOrSelfFocused = IsGameOrSelf(NativeMethods.GetForegroundWindow());
            _reconcileTimer.Tick += (_, _) => Evaluate(NativeMethods.GetForegroundWindow());
            _reconcileTimer.Start();
            _callback = OnForegroundChanged;
            _hook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _callback, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
            if (_hook == IntPtr.Zero)
                logger.LogWarning("Foreground hook could not be installed; game focus detection is inactive.");
        }

        private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime) =>
            Evaluate(hWnd);

        private void Evaluate(IntPtr hWnd)
        {
            // A null foreground window is a transient state during focus switches.
            if (hWnd == IntPtr.Zero) return;

            var focused = IsGameOrSelf(hWnd);
            if (focused == IsGameOrSelfFocused) return;

            IsGameOrSelfFocused = focused;
            FocusChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool IsGameOrSelf(IntPtr hWnd)
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == 0) return false;
            if (processId == _ownProcessId) return true;
            if (processId == _lastProcessId) return _lastProcessIsGameOrSelf;

            string processName;
            try
            {
                using var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                logger.LogDebug(ex, "Foreground process {ProcessId} could not be resolved", processId);
                return false;
            }

            var isGame = string.Equals(processName, GameProcessName, StringComparison.OrdinalIgnoreCase);
            _lastProcessId = processId;
            _lastProcessIsGameOrSelf = isGame;
            logger.LogDebug("Foreground: {ProcessName} ({ProcessId}) isGame={IsGame}", processName, processId, isGame);
            return isGame;
        }

        public void Dispose()
        {
            _reconcileTimer.Stop();
            if (_hook == IntPtr.Zero) return;
            NativeMethods.UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
