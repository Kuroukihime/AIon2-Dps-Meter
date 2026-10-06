using System.Windows;
using AionDpsMeter.Core.Windowing;

namespace AionDpsMeter.UI.Services.Windowing
{
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

        void SetClickThrough(WindowKey key, string? instanceId = null);

        void RestoreClickThrough(WindowKey key, string? instanceId = null);

        bool IsOpen(WindowKey key, string? instanceId = null);

        /// <summary>
        /// Positions the window over the game: at its saved game-relative spot, else at the default computed from its size,
        /// kept inside <paramref name="gameRect"/>. Returns the window's size, or <see cref="Size.Empty"/> when it is not open.
        /// </summary>
        Size PlaceOverGame(WindowKey key, Rect gameRect, Func<Size, Point> defaultTopLeft);
    }
}
