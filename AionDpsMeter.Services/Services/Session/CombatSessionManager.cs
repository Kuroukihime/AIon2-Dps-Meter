using AionDpsMeter.Core.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class CombatSessionManager
    {
        private readonly ConcurrentDictionary<int, TargetEntry> targetEntries = new();
        private readonly ActiveTargetResolver targetResolver;
        private readonly EntityTracker entityTracker;
        private readonly BuffEventManager buffEventManager = new();
        private readonly ILogger<CombatSessionManager> logger;
        private readonly Lock lockObject = new();


        public CombatSessionManager(EntityTracker entityTracker, ILoggerFactory loggerFactory)
        {
            this.entityTracker = entityTracker;
            targetResolver = new ActiveTargetResolver(entityTracker);
            logger = loggerFactory.CreateLogger<CombatSessionManager>();
            entityTracker.SummonRegistered += OnSummonRegistered;
        }

      

      
        public TargetCombatSession? GetActiveTargetSession()
        {
            lock (lockObject)
            {
                if (targetResolver.ActiveTargetId is not { } id) return null;
                return targetEntries.TryGetValue(id, out var entry) ? entry.CurrentSession : null;
            }
        }

      
        public IEnumerable<TargetCombatSession> GetTargetHistory(int targetId)
        {
            lock (lockObject)
            {
                return targetEntries.TryGetValue(targetId, out var entry)
                    ? entry.AllSessions.ToList()
                    : [];
            }
        }

        public IReadOnlyCollection<TargetEntry> AllTargetEntries
        {
            get { lock (lockObject) { return targetEntries.Values.ToList(); } }
        }

      
        public IReadOnlyList<HistorySessionSnapshot> GetHistorySnapshot()
        {
            lock (lockObject)
            {
                return targetEntries.Values
                    .SelectMany(e => e.AllSessions)
                    .OrderByDescending(s => s.LastHitTime)
                    .Select(s => HistorySessionSnapshot.From(s, buffEventManager))
                    .ToList();
            }
        }

        public IReadOnlyCollection<PlayerStats> PlayerStats
        {
            get { lock (lockObject) { return GetActiveTargetSession()?.GetPlayerStats() ?? []; } }
        }

        public IReadOnlyList<PlayerDamage> GetPlayerCombatLog(long playerId)
        {
            lock (lockObject)
            {
                return GetActiveTargetSession()?.GetCombatLog(playerId) ?? [];
            }
        }

        public IReadOnlyCollection<SkillStats> GetPlayerSkillStats(long playerId)
        {
            lock (lockObject)
            {
                return GetActiveTargetSession()?.GetSkillStats(playerId) ?? [];
            }
        }

        public IReadOnlyCollection<BuffStats> GetPlayerBuffStats(long playerId)
        {
            lock (lockObject)
            {
                var session = GetActiveTargetSession();
                if (session is null) return [];

                var sessionStart = session.SessionStart;
                var sessionEnd = session.LastHitTime;

                var buffs = buffEventManager.GetBuffEvents((int)playerId, sessionStart, sessionEnd);
                return BuffStatisticsCalculator.ComputeBuffStats(buffs, sessionStart, sessionEnd);
            }
        }

        public (IReadOnlyList<Models.DpsDataPoint> Points, IReadOnlyList<Models.BuffTimelineEntry> BuffTimeline, double TotalSec)
            GetPlayerGraphData(long playerId)
        {
            lock (lockObject)
            {
                var session = GetActiveTargetSession();
                if (session is null)
                    return ([], [], 0);

                var hits = session.GetCombatLog(playerId);
                if (hits.Count == 0)
                    return ([], [], 0);

                var sessionStart = session.SessionStart;
                var sessionEnd   = session.LastHitTime;
                var buffEvents   = buffEventManager.GetBuffEvents((int)playerId, sessionStart, sessionEnd);

                return GraphDataCalculator.Compute(hits, buffEvents);
            }
        }

        public TimeSpan GetCombatDuration()
        {
            lock (lockObject)
            {
                return GetActiveTargetSession()?.GetCombatDuration() ?? TimeSpan.Zero;
            }
        }

        public Mob? GetActiveTargetInfo()
        {
            lock (lockObject) { return targetResolver.GetActiveTargetMob(); }
        }

       

        public void ProcessDamageEvent(PlayerDamage damageEvent)
        {
            try
            {
                lock (lockObject)
                {
                    if (entityTracker.IsSummon(damageEvent.SourceEntity.Id) &&
                        !ResolveSummonSource(damageEvent))
                        return;
                    RouteToTargetEntry(damageEvent);

                    // Check all other entries for idle timeout on each new event
                    CheckIdleTimeouts(damageEvent.DateTime, excludeTargetId: damageEvent.TargetEntity.Id);

                    targetResolver.Update(targetEntries.Values, damageEvent.DateTime);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing damage event");
            }
        }

        public void ProcessBuffEvent(BuffEvent buffEvent)
        {
            try
            {
                lock (lockObject)
                {
                    buffEventManager.Add(buffEvent);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing buff event");
            }
        }

        public void Reset()
        {
            lock (lockObject) { ResetInternal(); }
        }

        

        private void RouteToTargetEntry(PlayerDamage damageEvent)
        {
            var entry = targetEntries.GetOrAdd(
                damageEvent.TargetEntity.Id,
                id => new TargetEntry(id, entityTracker));

            entry.AddDamage(damageEvent);
        }

        private void CheckIdleTimeouts(DateTime now, int excludeTargetId)
        {
            foreach (var (id, entry) in targetEntries)
            {
                if (id == excludeTargetId) continue;
                entry.CheckIdleTimeout(now);
            }
        }

        private bool ResolveSummonSource(PlayerDamage damageEvent)
        {
            var ownerId = entityTracker.GetSummonOwner(damageEvent.SourceEntity.Id);
            if (ownerId is null)
            {
                logger.LogError("Summon owner not found for summon entity {SummonId}", damageEvent.SourceEntity.Id);
                return false;
            }

            damageEvent.SourceEntity = new Player
            {
                CharacterClass = damageEvent.SourceEntity.CharacterClass,
                Icon = damageEvent.SourceEntity.Icon,
                Name = damageEvent.SourceEntity.Name,
                Id = ownerId.Value,
            };
            return true;
        }

        private void ResetInternal()
        {
            foreach (var entry in targetEntries.Values)
                entry.Reset();
            targetEntries.Clear();
            targetResolver.Reset();
            buffEventManager.Reset();
        }

        private void OnSummonRegistered(int summonId, int ownerId)
        {
            lock (lockObject)
            {
                int totalTransferred = 0;
                int entriesAffected  = 0;

                foreach (var (targetId, entry) in targetEntries)
                {
                    if (entry.CurrentSession is not { } current) continue;

                    int result = current.TransferSummonDamage(summonId, ownerId);
                    if (result == 0)
                    {
                        logger.LogWarning(
                            "Summon late-registration: summon {SummonId} found in target entry {TargetId} but owner player entity {OwnerId} is unknown. Skipping transfer.",
                            summonId, targetId, ownerId);
                    }
                    else if (result > 0)
                    {
                        totalTransferred += result;
                        entriesAffected++;
                        logger.LogInformation(
                            "Summon late-registration: transferred {Count} hit(s) from summon {SummonId} to owner {OwnerId} in target entry {TargetId}.",
                            result, summonId, ownerId, targetId);
                    }
                }

                if (totalTransferred == 0)
                    logger.LogDebug(
                        "Summon late-registration: summon {SummonId} registered with owner {OwnerId} — no prior damage hits found to transfer.",
                        summonId, ownerId);
                else
                    logger.LogInformation(
                        "Summon late-registration complete: summon {SummonId} → owner {OwnerId}. Total hits transferred: {Total} across {Entries} target entry(ies).",
                        summonId, ownerId, totalTransferred, entriesAffected);
            }
        }
    }
}