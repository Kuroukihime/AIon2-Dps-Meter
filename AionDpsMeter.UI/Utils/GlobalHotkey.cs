using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AionDpsMeter.UI.Utils
{
    public sealed class GlobalHotkey : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly Window _window;
        private HwndSource? _source;
        private int _id;
        private bool _registered;

        public event Action? HotkeyPressed;

        public GlobalHotkey(Window window)
        {
            _window = window;
        }

        public void Register(uint modifiers, uint vk, int id = 9001)
        {
            Unregister();
            if (vk == 0) return;

            _id = id;
            var helper = new WindowInteropHelper(_window);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source?.AddHook(WndProc);
            _registered = RegisterHotKey(helper.Handle, _id, modifiers, vk);
        }

        public void Unregister()
        {
            if (_registered && _source != null)
            {
                var helper = new WindowInteropHelper(_window);
                UnregisterHotKey(helper.Handle, _id);
                _source.RemoveHook(WndProc);
                _registered = false;
                _source = null;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose() => Unregister();
    }
}
