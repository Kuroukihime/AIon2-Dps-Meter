using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;
using AionDpsMeter.Services.Services.Settings;
using static System.Net.Mime.MediaTypeNames;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>
    /// Tracks all combat attempts (tries) against a single mob.
    /// Detects mob HP resets to automatically start a new <see cref="TargetCombatSession"/>.
    /// Exposes the current in-progress session and the full history of completed sessions.
    /// </summary>
    public sealed class TargetEntry
    {
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan ScarecrowIdleTimeout = TimeSpan.FromSeconds(5);

        private readonly EntityTracker entityTracker;
        private readonly Action<TargetCombatSession>? onSessionCompleted;

        public int TargetId { get; }

        public TargetCombatSession? CurrentSession { get; private set; }


        public IEnumerable<TargetCombatSession> AllSessions =>
            CurrentSession is null ? [] : [ CurrentSession];

        private readonly IAppSettingsService settingsService;

        public TargetEntry(
            int targetId,
            EntityTracker entityTracker,
            IAppSettingsService settingsService,
            Action<TargetCombatSession>? onSessionCompleted = null)
        {
            TargetId = targetId;
            this.entityTracker = entityTracker;
            this.settingsService = settingsService;
            this.onSessionCompleted = onSessionCompleted;
        }

        public void AddDamage(PlayerDamage damage)
        {
            var mob = entityTracker.GetTargetMob(damage.TargetEntity.Id) ?? damage.TargetEntity;

            if (CurrentSession is not null && CurrentSession.IsNewTry() || ShouldCompleteSession(damage.DateTime, mob))
            {
                CompleteCurrentSession();
                StartNewSession(mob, damage.DateTime);
            }
            else if (CurrentSession is null)
            {
                StartNewSession(mob, damage.DateTime);
            }

            CurrentSession!.AddDamage(damage);
        }


        public void TryAddBuff(BuffEvent buffEvent)
        {
            foreach (var session in AllSessions.Where(r => !r.IsCompleted))
            {
                session.ProcessBuffEvent(buffEvent);
            }
        }


        public void CheckIdleTimeout(DateTime now)
        {
            if (CurrentSession is null || CurrentSession.IsCompleted) return;

            if (ShouldCompleteSession(now)) CompleteCurrentSession();
        }

        public void CompleteActiveSession()
        {
            if (CurrentSession is null || CurrentSession.IsCompleted) return;
            CompleteCurrentSession();
        }


        private bool ShouldCompleteSession(DateTime now, Mob? currentMobState = null)
        {
            if (CurrentSession is null || CurrentSession.IsCompleted) return false;

            if(currentMobState?.Name == "Training Scarecrow") 
                return now - CurrentSession.LastHitTime > ScarecrowIdleTimeout;

            return now - CurrentSession.LastHitTime > IdleTimeout;
        }

        public int CountRecentHits(DateTime cutoff)
            => CurrentSession?.CountRecentHits(cutoff) ?? 0;

        public DateTime? GetUserLastHitTime()
            => CurrentSession?.GetUserLastHitTime();

        public void Reset()
        {
            CurrentSession?.Reset();
            CurrentSession = null;
        }


        private bool IsNewTry(Mob currentMobState)
        {
            if (CurrentSession is null) return false;

            if (currentMobState.Name == "Training Scarecrow") return false;

            // HP increasing means the mob respawned / fight restarted
            var lastKnownHp = CurrentSession.TargetInfo.HpCurrent;
            return currentMobState.HpCurrent > lastKnownHp && lastKnownHp > 0;
        }

        private void CompleteCurrentSession()
        {
            if (CurrentSession is null) return;
            CurrentSession.Complete();
            if(CurrentSession.TargetInfo.IsBoss) onSessionCompleted?.Invoke(CurrentSession);
            CurrentSession = null;
        }

        private void StartNewSession(Mob mob, DateTime at)
        {
            CurrentSession = new TargetCombatSession(mob, at, entityTracker, settingsService);
        }
    }
}