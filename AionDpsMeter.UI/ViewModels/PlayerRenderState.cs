namespace AionDpsMeter.UI.ViewModels
{
    public class PlayerRenderState
    {
        public long PlayerId { get; set; }
        public bool IsUser { get; set; }
        public string ClassId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;

        public string PlayerNameDisplay { get; set; } = string.Empty;
        public string DeathsDisplay { get; set; } = string.Empty;

        public long TotalDamage { get; set; }
        public string TotalDamageFormatted { get; set; } = string.Empty;
        public string DpsFormatted { get; set; } = string.Empty;
        public double DamagePercentage { get; set; }
        public string CombatPower { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string? ClassIcon { get; set; }
        public double CriticalRate { get; set; }

        public double VisualAbsolutePercentage { get; set; }
        public double VisualRelativePercentage { get; set; }
        public double EffectivePercentage { get; set; }
    }
}
