using AionDpsMeter.Core.Models;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Centralized storage for all buff events.  
    /// Allows querying buffs by entity and time range so that sessions
    /// can retrieve buff data regardless of when the buff was applied.
    /// Not thread-safe on its own — callers must synchronise externally.
    /// </summary>
    public sealed class BuffEventManager
    {
        private readonly List<BuffEvent> allEvents = new();

        public void Add(BuffEvent buffEvent)
        {
            allEvents.Add(buffEvent);
        }

        /// <summary>
        /// Returns buff events for <paramref name="entityId"/> whose active period
        /// overlaps <c>[from, to]</c>.
        /// </summary>
        public IReadOnlyList<BuffEvent> GetBuffEvents(int entityId, DateTime from, DateTime to)
        {
            var result = new List<BuffEvent>();
            foreach (var e in allEvents)
            {
                if (e.EntityId != entityId) continue;

                var buffEnd = e.AppliedAt.AddMilliseconds(e.DurationMs);
                if (e.AppliedAt < to && buffEnd > from)
                    result.Add(e);
            }
            return result;
        }

        public void Reset()
        {
            allEvents.Clear();
        }
    }
}
