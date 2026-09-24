using System.Windows;
using AionDpsMeter.Core.Windowing;

namespace AionDpsMeter.UI.Services.Windowing
{
    public interface IWindowManagerService
    {
        public event EventHandler? CloseAppCommand;
        public void CloseApplication();

        void Open(WindowKey key, Window window, bool isSingleton, string? instanceId = null, Window? owner = null, WindowPersistenceMode persistenceMode = WindowPersistenceMode.None);

        void Hide(WindowKey key, string? instanceId = null);

        void Minimize(WindowKey key, string? instanceId = null);

        void Close(WindowKey key, string? instanceId = null);

        void CloseAll();

        void Focus(WindowKey key, string? instanceId = null);

        void Drag(WindowKey key, string? instanceId = null);

        void SetClickThrough(WindowKey key, string? instanceId = null);

        void RestoreClickThrough(WindowKey key, string? instanceId = null);

        bool IsOpen(WindowKey key, string? instanceId = null);
    }
}
