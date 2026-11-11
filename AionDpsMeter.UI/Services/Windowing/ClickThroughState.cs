namespace AionDpsMeter.UI.Services.Windowing;

internal sealed class ClickThroughState
{
    public required IntPtr WindowHandle { get; init; }

    public required long WindowExtendedStyle { get; init; }

    public bool IsEnabled { get; set; }
}
