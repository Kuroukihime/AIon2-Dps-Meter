namespace AionDpsMeter.Services.Services.Settings
{
    public interface IAppSettingsService
    {
        bool IsPacketLoggingEnabled { get; set; }
        bool IsNicknameHidden { get; set; }
        bool BossOnlyCapture { get; set; }
        bool GroupSummonDamage { get; set; }
        bool OneMinuteDummyMode { get; set; }
        bool ShowPlayerDeaths { get; set; }
        int HistoryRetantionPeriod { get; set; }
        double WindowOpacity { get; set; }
        string? BackgroundImagePath { get; set; }
        bool RelativeProgressBar { get; set; }

        string ToggleVisibilityHotkey { get; set; }

        // Main window position & size
        double? WindowLeft { get; set; }
        double? WindowTop { get; set; }
        double? WindowWidth { get; set; }
        double? WindowHeight { get; set; }
        double PlayerRowScale { get; set; }
        int UiStyle { get; set; }

        double StatCalcCritChance { get; set; }
        double StatCalcBackAttackRate { get; set; }
        double StatCalcFrontAttackRate { get; set; }
        string StatCalcAttackType { get; set; }
        double StatCalcAttackIncreaseCombatPercent { get; set; }
        double StatCalcPartyDamageBoost { get; set; }
        double StatCalcBossDamageTolerance { get; set; }
        double StatCalcPartySmiteBuff { get; set; }
        double StatCalcBossSmiteResist { get; set; }

        event EventHandler SettingsChanged;
    }
}
