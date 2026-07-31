namespace AionDpsMeter.Services.Services.Session
{
    public sealed class HistorySessionPageResult
    {
        public IReadOnlyList<HistorySessionListItem> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
    }
}
