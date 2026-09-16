using System.Windows;

namespace AionDpsMeter.UI.Services.Windowing
{
    public enum WindowKey
    {
        Main,
        Settings,
        History,
        StatEfficiencyCalculator,
        WhatsNew,
        PlayerDetails,
    }

    public interface IWindowManagerService
    {
        public event EventHandler? CloseAppCommand;
        public void CloseApplication();

        void Open(WindowKey key, Window window, bool isSingleton, string? instanceId = null, Window? owner = null);

        void Hide(WindowKey key, string? instanceId = null);

        void Minimize(WindowKey key, string? instanceId = null);

        void Close(WindowKey key, string? instanceId = null);

        void CloseAll();

        void Focus(WindowKey key, string? instanceId = null);

        void Drag(WindowKey key, string? instanceId = null);

        bool IsOpen(WindowKey key, string? instanceId = null);
    }
}
