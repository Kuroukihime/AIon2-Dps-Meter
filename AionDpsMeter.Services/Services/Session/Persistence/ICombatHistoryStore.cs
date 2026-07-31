namespace AionDpsMeter.Services.Services.Session.Persistence
{
    public interface ICombatHistoryStore
    {
        void Save(HistorySessionSnapshot snapshot);
        IReadOnlyList<HistorySessionListItem> GetSessionList();
        int GetSessionCount(DateTime? dateFrom, DateTime? dateTo, string? bossNameContains, long minTotalDamage, IReadOnlySet<Guid>? excludeSessionIds = null);
        IReadOnlyList<HistorySessionListItem> GetSessionPage(
            DateTime? dateFrom,
            DateTime? dateTo,
            string? bossNameContains,
            long minTotalDamage,
            int skip,
            int take,
            IReadOnlySet<Guid>? excludeSessionIds = null);
        HistorySessionSnapshot? GetSession(Guid sessionId);
    }
}
