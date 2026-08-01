namespace AionDpsMeter.Services.Services.Session.Persistence
{
    public sealed class CombatSessionEntity
    {
        public Guid SessionId { get; set; }
        public int TargetId { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public int TargetHpTotal { get; set; }
        public DateTime SessionStart { get; set; }
        public DateTime SessionEnd { get; set; }
        public SessionState State { get; set; }
        public long TotalDamage { get; set; }
        public int PlayerCount { get; set; }

        public string PlayerStatsJson { get; set; } = "[]";
        public string SkillStatsByPlayerJson { get; set; } = "{}";
        public string BuffStatsByPlayerJson { get; set; } = "{}";
        public string HitsByPlayerJson { get; set; } = "{}";
        public string BuffEventsByPlayerJson { get; set; } = "{}";
    }
}
