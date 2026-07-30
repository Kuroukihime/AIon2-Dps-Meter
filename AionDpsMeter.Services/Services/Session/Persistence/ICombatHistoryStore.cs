namespace AionDpsMeter.Services.Services.Session.Persistence
{
    public interface ICombatHistoryStore
    {
        void Save(HistorySessionSnapshot snapshot);
        IReadOnlyList<HistorySessionListItem> GetSessionList();
        HistorySessionSnapshot? GetSession(Guid sessionId);
    }
}
