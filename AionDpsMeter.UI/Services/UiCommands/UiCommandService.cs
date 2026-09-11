using AionDpsMeter.UI.Services.UiCommands;

namespace AionDpsMeter.UI.UiCommands
{
    public sealed class UiCommandService : IUiCommandService
    {
        public event EventHandler<UiCommandRequest>? CommandRequested;

        public void Request(UiCommandRequest request)
        {
            CommandRequested?.Invoke(this, request);
        }
    }
}
