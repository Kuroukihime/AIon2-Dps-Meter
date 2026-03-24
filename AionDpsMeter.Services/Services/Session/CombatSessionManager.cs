using AionDpsMeter.Core.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class CombatSessionManager
    {
        private static readonly TimeSpan HardResetThreshold = TimeSpan.FromSeconds(40);
        private static readonly TimeSpan SoftResetThreshold = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan ActiveTargetWindow = TimeSpan.FromSeconds(5);

        private readonly ConcurrentDictionary<long, PlayerSession> playerSessions = new();
        private readonly HashSet<int> knownTargetIds = new();
        private readonly Dictionary<int, int> targetCountBuffer = new();
        private readonly object lockObject = new();
        private DateTime? combatStartTime;
        private DateTime? combatLastHitTime;
        private int? activeTargetId;
        private readonly ILogger<CombatSessionManager> logger;
        private readonly ILoggerFactory loggerFactory;

        public event EventHandler? CombatAutoReset;
        public CombatSessionManager(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger<CombatSessionManager>();
        }

        private long TotalCombatDamage => playerSessions.Values.Sum(s => s.Stats.TotalDamage);

        public IReadOnlyCollection<PlayerStats> PlayerStats
        {
            get
            {
                lock (lockObject)
                {
                    return playerSessions.Values
                        .Where(s => s.Stats.HitCount > 0)
                        .Select(s => s.Stats)
                        .ToList();
                }
            }
        }

        public IReadOnlyList<PlayerDamage> GetPlayerCombatLog(long playerId)
        {
            lock (lockObject)
            {
                if (activeTargetId != null && playerSessions.TryGetValue(playerId, out var session))
                {
                    var filtered = session.GetDamageHistory(activeTargetId.Value);
                    var result = new List<PlayerDamage>(filtered);
                    result.Reverse();
                    return result;
                }
                return Array.Empty<PlayerDamage>();
            }
        }

        public void ProcessDamageEvent(PlayerDamage damageEvent)
        {
            try
            {
                lock (lockObject)
                {
                    if (combatLastHitTime != null)
                    {
                        var gap = damageEvent.DateTime - combatLastHitTime.Value;
                        if (gap > HardResetThreshold ||
                            (gap > SoftResetThreshold && !knownTargetIds.Contains(damageEvent.TargetEntity.Id)))
                        {
                            ResetInternal();
                            CombatAutoReset?.Invoke(this, EventArgs.Empty);
                        }
                    }

                    combatStartTime ??= damageEvent.DateTime;
                    if (combatLastHitTime == null || damageEvent.DateTime > combatLastHitTime)
                        combatLastHitTime = damageEvent.DateTime;

                    knownTargetIds.Add(damageEvent.TargetEntity.Id);

                    if (damageEvent.Skill.IsEntity)
                    {
                        ProcessEntityDamageEvent(damageEvent);
                        return;
                    }

                    AddDamageEvent(damageEvent);
                    RecalculateStatistics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }

        private void ProcessEntityDamageEvent(PlayerDamage damageEvent)
        {
            var entityClassId = damageEvent.Skill.ClassId;
            var registeredEntitiesOfThisClass = playerSessions.Values.Where(r => r.ClassId == entityClassId).ToList();
            if (registeredEntitiesOfThisClass.Count == 1)
            {
                registeredEntitiesOfThisClass.First().AddDamage(damageEvent);
                return;
            }
            // If there are multiple entities of the same class, we cannot determine which one dealt the damage.
            // In this case, we will register damage event as new entity.
            AddDamageEvent(damageEvent);
            RecalculateStatistics();
        }

        private void AddDamageEvent(PlayerDamage damageEvent)
        {
            var session = playerSessions.GetOrAdd(damageEvent.SourceEntity.Id, _ => new PlayerSession(damageEvent, loggerFactory.CreateLogger<PlayerSession>()));
            session.AddDamage(damageEvent);
        }

        public IReadOnlyCollection<SkillStats> GetPlayerSkillStats(long playerId)
        {
            lock (lockObject)
            {
                if (activeTargetId != null && playerSessions.TryGetValue(playerId, out var session))
                    return session.GetSkillStats(activeTargetId.Value);
                return Array.Empty<SkillStats>();
            }
        }

        private int? DetermineActiveTargetId()
        {
            if (combatLastHitTime == null) return null;

            var cutoff = combatLastHitTime.Value - ActiveTargetWindow;
            targetCountBuffer.Clear();

            foreach (var session in playerSessions.Values)
            {
                session.CountRecentTargetHits(cutoff, targetCountBuffer);
            }

            if (targetCountBuffer.Count == 0) return null;

            int bestTargetId = 0;
            int bestCount = 0;
            foreach (var kvp in targetCountBuffer)
            {
                if (kvp.Value > bestCount)
                {
                    bestCount = kvp.Value;
                    bestTargetId = kvp.Key;
                }
            }

            return bestTargetId;
        }

        private void RecalculateStatistics()
        {
            if (combatStartTime == null) return;

            activeTargetId = DetermineActiveTargetId();
            if (activeTargetId == null) return;

            int targetId = activeTargetId.Value;

            foreach (var playerSess in playerSessions.Values)
            {
                playerSess.UpdateStats(0, targetId);
            }

            var totalDamage = TotalCombatDamage;
            foreach (var playerSess in playerSessions.Values)
            {
                playerSess.UpdateStats(totalDamage, targetId);
            }
        }

        public TimeSpan GetCombatDuration()
        {
            lock (lockObject)
            {
                if (combatStartTime == null || combatLastHitTime == null) return TimeSpan.Zero;

                var duration = combatLastHitTime.Value - combatStartTime.Value;
                return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
            }
        }

        private void ResetInternal()
        {
            foreach (var session in playerSessions.Values)
                session.Reset();
            playerSessions.Clear();
            knownTargetIds.Clear();
            targetCountBuffer.Clear();
            activeTargetId = null;
            combatStartTime = null;
            combatLastHitTime = null;
        }

        public void Reset()
        {
            lock (lockObject)
            {
                ResetInternal();
            }
        }

        public ActiveTargetInfo? GetActiveTargetInfo()
        {
            lock (lockObject)
            {
                if (activeTargetId == null) return null;

                int targetId = activeTargetId.Value;

                // Find the Mob from any player session's damage history
                foreach (var session in playerSessions.Values)
                {
                    var damageHistory = session.GetDamageHistory(targetId);
                    if (damageHistory.Count > 0)
                    {
                        var mob = damageHistory[0].TargetEntity;
                        return new ActiveTargetInfo
                        {
                            TargetId = targetId,
                            Name = mob.Name,
                            HpTotal = mob.HpTotal,
                            HpCurrent = mob.HpCurrent
                        };
                    }
                }

                return null;
            }
        }
    }
}
