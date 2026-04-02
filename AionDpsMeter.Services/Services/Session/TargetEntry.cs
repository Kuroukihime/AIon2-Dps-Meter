using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Tracks all combat attempts (tries) against a single mob.
    /// Detects mob HP resets to automatically start a new <see cref="TargetCombatSession"/>.
    /// Exposes the current in-progress session and the full history of completed sessions.
    /// </summary>
    public sealed class TargetEntry
    {
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(20);

        private readonly EntityTracker entityTracker;
        private readonly List<TargetCombatSession> history = new();

        public int TargetId { get; }

        /// <summary>The session currently receiving damage. Null before the first hit.</summary>
        public TargetCombatSession? CurrentSession { get; private set; }

        /// <summary>All completed sessions, ordered oldest-first.</summary>
        public IReadOnlyList<TargetCombatSession> History => history;

        /// <summary>Current session + all history combined, ordered oldest-first.</summary>
        public IEnumerable<TargetCombatSession> AllSessions =>
            CurrentSession is null ? history : [.. history, CurrentSession];

        public TargetEntry(int targetId, EntityTracker entityTracker)
        {
            TargetId = targetId;
            this.entityTracker = entityTracker;
        }

        /// <summary>
        /// Routes a damage event to the correct session.
        /// Creates a new session when:
        /// <list type="bullet">
        ///   <item>No session exists yet.</item>
        ///   <item>The mob's HP increased since the last recorded value (new try of the same boss).</item>
        /// </list>
        /// </summary>
        public void AddDamage(PlayerDamage damage)
        {
            var mob = entityTracker.GetTargetMob(damage.TargetEntity.Id) ?? damage.TargetEntity;

            if (CurrentSession is not null && IsNewTry(mob))
            {
                CompleteCurrentSession(damage.DateTime);
                StartNewSession(mob, damage.DateTime);
            }
            else if (CurrentSession is null)
            {
                StartNewSession(mob, damage.DateTime);
            }

            CurrentSession!.AddDamage(damage);
        }

        /// <summary>
        /// Checks whether the current session has gone idle (no hits for <see cref="IdleTimeout"/>)
        /// and marks it completed if so.
        /// </summary>
        public void CheckIdleTimeout(DateTime now)
        {
            if (CurrentSession is null || CurrentSession.IsCompleted) return;

            if (now - CurrentSession.LastHitTime > IdleTimeout)
                CompleteCurrentSession(now);
        }

        /// <summary>
        /// Returns recent hit count for the current session only —
        /// used by <see cref="ActiveTargetResolver"/>.
        /// </summary>
        public int CountRecentHits(DateTime cutoff)
            => CurrentSession?.CountRecentHits(cutoff) ?? 0;

        public void Reset()
        {
            CurrentSession?.Reset();
            CurrentSession = null;
            foreach (var s in history) s.Reset();
            history.Clear();
        }

        // ── Private ────────────────────────────────────────────────────────────

        private bool IsNewTry(Mob currentMobState)
        {
            if (CurrentSession is null) return false;

            if (currentMobState.Name == "Training Scarecrow") return false;

            // HP increasing means the mob respawned / fight restarted
            var lastKnownHp = CurrentSession.TargetInfo.HpCurrent;
            return currentMobState.HpCurrent > lastKnownHp && lastKnownHp > 0;
        }

        private void CompleteCurrentSession(DateTime at)
        {
            if (CurrentSession is null) return;
            CurrentSession.Complete(at);
            history.Add(CurrentSession);
            CurrentSession = null;
        }

        private void StartNewSession(Mob mob, DateTime at)
        {
            CurrentSession = new TargetCombatSession(mob, at, entityTracker);
        }
    }
}