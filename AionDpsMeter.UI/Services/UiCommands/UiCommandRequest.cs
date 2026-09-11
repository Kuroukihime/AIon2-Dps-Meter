using AionDpsMeter.UI.UiCommands;

namespace AionDpsMeter.UI.Services.UiCommands
{
    public sealed record UiCommandRequest(
        UiCommandType Command,
        long? PlayerId = null,
        string? PlayerName = null);
}
