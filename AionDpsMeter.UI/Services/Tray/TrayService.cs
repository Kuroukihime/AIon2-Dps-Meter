using AionDpsMeter.UI.Services.Windowing;
using Forms = System.Windows.Forms;

namespace AionDpsMeter.UI.Services.Tray
{
    public sealed class TrayService : IDisposable
    {
        private const string TooltipText = "AION2 DPS Meter";

        private readonly WindowVisibilityService visibility;
        private readonly Forms.NotifyIcon _notifyIcon;

        public TrayService(IWindowManagerService windowManager, WindowVisibilityService visibility, WindowHelper windowHelper)
        {
            this.visibility = visibility;

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Show", null, (_, _) => Restore());
            menu.Items.Add("Settings", null, (_, _) =>
            {
                Restore();
                windowHelper.OpenSettings();
            });
            menu.Items.Add("Exit", null, (_, _) => windowManager.CloseApplication());

            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = LoadAppIcon(),
                Text = TooltipText,
                ContextMenuStrip = menu,
                Visible = true
            };
            _notifyIcon.DoubleClick += (_, _) => Restore();
        }

        public void HideToTray() => visibility.Hide(HideReason.Tray);

        public void Restore() => visibility.RestoreAll();

        public void Toggle()
        {
            if (visibility.IsHiddenBy(HideReason.Tray)) visibility.Clear(HideReason.Tray);
            else visibility.Hide(HideReason.Tray);
        }

        public void ShowNotification(string title, string text) =>
            _notifyIcon.ShowBalloonTip(5000, title, text, Forms.ToolTipIcon.Info);

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
        }

        private static System.Drawing.Icon LoadAppIcon() =>
            (Environment.ProcessPath is { } path ? System.Drawing.Icon.ExtractAssociatedIcon(path) : null)
            ?? System.Drawing.SystemIcons.Application;
    }
}
