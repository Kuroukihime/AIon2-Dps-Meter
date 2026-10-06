using System.Diagnostics;
using System.Windows.Threading;
using AionDpsMeter.UI.Services.Windowing.Native;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.UI.Services.Windowing
{
    public enum ForegroundKind { Other, Game, Self }

    /// <summary>
    /// Tracks whether the foreground window belongs to the game, to this app, or to something else.
    /// Purely event-driven: the foreground WinEvent, plus a location-change WinEvent scoped to the game's process.
    /// </summary>
    public sealed class GameFocusWatcher(ILogger<GameFocusWatcher> logger) : IDisposable
    {
        private const string GameProcessName = "AION2";

        private readonly int _ownProcessId = Environment.ProcessId;

        // Held in fields: the native hooks call these delegates, so they must outlive the hooks.
        private NativeMethods.WinEventProc? _callback;
        private NativeMethods.WinEventProc? _moveCallback;
        private IntPtr _hook;
        private IntPtr _moveHook;
        private int _moveHookProcessId;
        private readonly DispatcherTimer _moveDebounce = new() { Interval = TimeSpan.FromMilliseconds(50) };


        private uint _lastProcessId;
        private bool _lastProcessIsGame;
        private int _gameProcessId;

        public ForegroundKind Foreground { get; private set; } = ForegroundKind.Other;

        /// <summary>The game's top-level window, as last seen in the foreground.</summary>
        public IntPtr GameWindow { get; private set; }

        public event EventHandler? ForegroundChanged;

        /// <summary>Raised when the game takes the foreground; fires before <see cref="ForegroundChanged"/>.</summary>
        public event EventHandler? GameFocused;

        /// <summary>Raised (coalesced) while the game window moves or resizes, e.g. to another monitor.</summary>
        public event EventHandler? GameMoved;

        public void Start()
        {
            if (_hook != IntPtr.Zero) return;

            _moveCallback = OnGameLocationChanged;
            _moveDebounce.Tick += (_, _) =>
            {
                _moveDebounce.Stop();
                GameMoved?.Invoke(this, EventArgs.Empty);
            };

            Evaluate(NativeMethods.GetForegroundWindow(), "start");
            IsGameRunning();
            _callback = OnForegroundChanged;
            _hook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _callback, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
            if (_hook == IntPtr.Zero)
                logger.LogWarning("Foreground hook could not be installed; game focus detection is inactive.");
        }

        public bool IsGameRunning()
        {
            if (_gameProcessId != 0)
            {
                try
                {
                    using var cached = Process.GetProcessById(_gameProcessId);
                    if (!cached.HasExited) return true;
                }
                catch (ArgumentException)
                {
                    // Exited since the last check; fall through to a fresh lookup.
                }

                TrackGameProcess(0);
            }

            var running = Process.GetProcessesByName(GameProcessName);
            try
            {
                var withWindow = running.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero) ?? running.FirstOrDefault();
                TrackGameProcess(withWindow?.Id ?? 0);
                return withWindow is not null;
            }
            finally
            {
                foreach (var process in running) process.Dispose();
            }
        }

        /// <summary>
        /// The game's main window: the last one seen in the foreground, else the main window of a running game process.
        /// Returns <see cref="IntPtr.Zero"/> when the game is not running.
        /// </summary>
        public IntPtr FindGameWindow()
        {
            if (GameWindow != IntPtr.Zero && NativeMethods.IsWindow(GameWindow)) return GameWindow;

            var running = Process.GetProcessesByName(GameProcessName);
            try
            {
                return running.Select(p => p.MainWindowHandle).FirstOrDefault(h => h != IntPtr.Zero);
            }
            finally
            {
                foreach (var process in running) process.Dispose();
            }
        }

        private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime) =>
            Evaluate(hWnd, "hook");

        private void Evaluate(IntPtr hWnd, string source)
        {
            // A null foreground window is a transient state during focus switches.
            if (hWnd == IntPtr.Zero) return;

            var kind = Classify(hWnd);
            if (kind == Foreground) return;

            Foreground = kind;

            if (kind == ForegroundKind.Game)
            {
                GameWindow = hWnd;
                GameFocused?.Invoke(this, EventArgs.Empty);
            }

            ForegroundChanged?.Invoke(this, EventArgs.Empty);
        }

        private ForegroundKind Classify(IntPtr hWnd)
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == 0) return ForegroundKind.Other;
            if (processId == _ownProcessId) return ForegroundKind.Self;
            if (processId == _lastProcessId) return _lastProcessIsGame ? ForegroundKind.Game : ForegroundKind.Other;

            string processName;
            try
            {
                using var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                logger.LogDebug(ex, "Foreground process {ProcessId} could not be resolved", processId);
                return ForegroundKind.Other;
            }

            var isGame = string.Equals(processName, GameProcessName, StringComparison.OrdinalIgnoreCase);
            _lastProcessId = processId;
            _lastProcessIsGame = isGame;
            if (isGame) TrackGameProcess((int)processId);
            logger.LogDebug("Foreground: {ProcessName} ({ProcessId}) isGame={IsGame}", processName, processId, isGame);
            return isGame ? ForegroundKind.Game : ForegroundKind.Other;
        }

        /// <summary>
        /// Remembers the game's process and scopes the location-change hook to it, so only the game's own moves are reported.
        /// </summary>
        private void TrackGameProcess(int processId)
        {
            _gameProcessId = processId;
            if (processId == _moveHookProcessId || _moveCallback is null) return;

            if (_moveHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_moveHook);
                _moveHook = IntPtr.Zero;
            }

            _moveHookProcessId = processId;
            if (processId == 0) return;

            _moveHook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_LOCATIONCHANGE, NativeMethods.EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero, _moveCallback, (uint)processId, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
            if (_moveHook == IntPtr.Zero)
                logger.LogWarning("Game move hook could not be installed; windows follow the game only on focus.");
        }

        private void OnGameLocationChanged(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            // Location changes also fire for carets and child objects; only the game's visible top-level windows matter.
            // Any of them counts, so a recreated or swapped game window is picked up too.
            if (idObject != NativeMethods.OBJID_WINDOW || idChild != 0 || hWnd == IntPtr.Zero) return;

            var isTopLevel = NativeMethods.GetAncestor(hWnd, NativeMethods.GA_ROOT) == hWnd;
            var isVisible = NativeMethods.IsWindowVisible(hWnd);

            if (!isTopLevel || !isVisible) return;

            GameWindow = hWnd;

            _moveDebounce.Stop();
            _moveDebounce.Start();
        }

        public void Dispose()
        {
            _moveDebounce.Stop();
            if (_moveHook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_moveHook);
                _moveHook = IntPtr.Zero;
            }

            if (_hook == IntPtr.Zero) return;
            NativeMethods.UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
