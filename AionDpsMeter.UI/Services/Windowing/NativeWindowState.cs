namespace AionDpsMeter.UI.Services.Windowing.Native;

internal sealed record NativeWindowState(
    IntPtr Handle,
    long ExtendedStyle,
    uint Style,
    bool WasEnabled)
{
    public static NativeWindowState Capture(IntPtr handle) =>
        new(
            handle,
            NativeWindowHelper.GetExtendedStyle(handle),
            NativeWindowHelper.GetStyle(handle),
            NativeWindowHelper.IsEnabled(handle));
}
