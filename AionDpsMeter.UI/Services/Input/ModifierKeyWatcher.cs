using System.Runtime.InteropServices;
using System.Windows.Threading;
using AionDpsMeter.Services.Services.Settings;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.UI.Services.Input
{
    /// <summary>
    /// Reports whether the configured "move overlays" modifier is held, via a low-level keyboard hook.
    /// It only inspects that modifier and always passes every key on unchanged.
    /// </summary>
    public sealed class ModifierKeyWatcher(IAppSettingsService settingsService, ILogger<ModifierKeyWatcher> logger) : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_LSHIFT = 0xA0, VK_RSHIFT = 0xA1;
        private const int VK_LCONTROL = 0xA2, VK_RCONTROL = 0xA3;
        private const int VK_LMENU = 0xA4, VK_RMENU = 0xA5;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        // Held in a field: the native hook calls this delegate, so it must outlive the hook.
        private LowLevelKeyboardProc? _proc;
        private IntPtr _hook;

        public bool IsHeld { get; private set; }

        public event EventHandler? HeldChanged;

        public void Start()
        {
            if (_hook != IntPtr.Zero) return;

            _proc = OnKeyboardEvent;
            _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
            if (_hook == IntPtr.Zero)
                logger.LogWarning("Keyboard hook could not be installed (error {Error}); overlays can only be moved while Settings is open.",
                    Marshal.GetLastWin32Error());
        }

        private IntPtr OnKeyboardEvent(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsWatchedKey(Marshal.ReadInt32(lParam)))
            {
                var down = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
                if (down != IsHeld)
                {
                    IsHeld = down;
                    // Return to the OS immediately; Windows drops low-level hooks that respond slowly.
                    _dispatcher.BeginInvoke(() => HeldChanged?.Invoke(this, EventArgs.Empty));
                }
            }

            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private bool IsWatchedKey(int vk) => settingsService.OverlayMoveKey switch
        {
            "Shift" => vk is VK_LSHIFT or VK_RSHIFT,
            "Alt" => vk is VK_LMENU or VK_RMENU,
            _ => vk is VK_LCONTROL or VK_RCONTROL
        };

        public void Dispose()
        {
            if (_hook == IntPtr.Zero) return;
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
