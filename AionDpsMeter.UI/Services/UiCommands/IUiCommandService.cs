namespace AionDpsMeter.UI.Services.UiCommands
{
    public interface IUiCommandService
    {
        event EventHandler<UiCommandRequest>? CommandRequested;

        void Request(UiCommandRequest request);
    }
}
