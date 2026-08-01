namespace AionDpsMeter.Services.Services.Session
{
    public sealed class HistorySessionListItem
    {
        public Guid SessionId { get; init; }
        public int TargetId { get; init; }
        public string TargetName { get; init; } = string.Empty;
        public int TargetHpTotal { get; init; }
        public DateTime SessionStart { get; init; }
        public DateTime SessionEnd { get; init; }
        public SessionState State { get; init; }
        public long TotalDamage { get; init; }
        public int PlayerCount { get; init; }

        public TimeSpan Duration => SessionEnd > SessionStart ? SessionEnd - SessionStart : TimeSpan.Zero;
    }
}
