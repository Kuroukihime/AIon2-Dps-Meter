namespace AionDpsMeter.Services.Services.Settings
{
    public interface IAppSettingsService
    {
        bool IsPacketLoggingEnabled { get; set; }
        bool IsNicknameHidden { get; set; }
        bool BossOnlyCapture { get; set; }
        int HistoryDamageThreshold { get; set; }
        double WindowOpacity { get; set; }
        string? BackgroundImagePath { get; set; }
        bool RelativeProgressBar { get; set; }

        string ToggleVisibilityHotkey { get; set; }

        // Main window position & size
        double? WindowLeft { get; set; }
        double? WindowTop { get; set; }
        double? WindowWidth { get; set; }
        double? WindowHeight { get; set; }

        event EventHandler SettingsChanged;
    }
}
