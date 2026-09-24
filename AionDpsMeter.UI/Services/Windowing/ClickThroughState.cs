using AionDpsMeter.UI.Services.Windowing.Native;

namespace AionDpsMeter.UI.Services.Windowing;

internal sealed class ClickThroughState
{
    public required IntPtr WindowHandle { get; init; }

    public required long WindowExtendedStyle { get; init; }

    public List<NativeWindowState> ChildWindows { get; } = [];

    public bool IsEnabled { get; set; }
}
