namespace AionDpsMeter.Services.Services.Session
{
    public sealed class HistorySessionQuery
    {
        public DateTime? DateFrom { get; init; }
        public DateTime? DateTo { get; init; }
        public string? BossNameContains { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 100;
    }
}
