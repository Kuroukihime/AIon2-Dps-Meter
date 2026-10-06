using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AionDpsMeter.UI.Services.Windowing
{
    /// <summary>
    /// Stops Windows Snap from resizing a window when it is dropped at a screen edge (full height, half width, maximize).
    /// Size changes are only accepted during a user resize (the grip), which Windows announces with WM_SIZING.
    /// </summary>
    internal sealed class SnapGuard
    {
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_SIZING = 0x0214;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int SC_MAXIMIZE = 0xF030;
        private const uint SWP_NOSIZE = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd, hwndInsertAfter;
            public int x, y, cx, cy;
            public uint flags;
        }

        private bool _userResizing;

        public static void Attach(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(new SnapGuard().WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_SIZING:
                    _userResizing = true;
                    break;

                case WM_EXITSIZEMOVE:
                    _userResizing = false;
                    break;

                case WM_SYSCOMMAND when (wParam.ToInt64() & 0xFFF0) == SC_MAXIMIZE:
                    handled = true;
                    break;

                case WM_WINDOWPOSCHANGING when !_userResizing:
                    var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    if ((pos.flags & SWP_NOSIZE) == 0)
                    {
                        pos.flags |= SWP_NOSIZE;
                        Marshal.StructureToPtr(pos, lParam, fDeleteOld: false);
                    }
                    break;
            }

            return IntPtr.Zero;
        }
    }
}
