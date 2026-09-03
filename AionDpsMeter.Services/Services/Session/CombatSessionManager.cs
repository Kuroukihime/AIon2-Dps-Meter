using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;
using AionDpsMeter.Services.Services.Session.Persistence;
using AionDpsMeter.Services.Services.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class CombatSessionManager
    {
        public event EventHandler<int>? PingUpdated;

        private readonly ConcurrentDictionary<int, TargetEntry> targetEntries = new();
        private readonly ActiveTargetResolver targetResolver;
        private readonly EntityTracker entityTracker;
        private readonly ILogger<CombatSessionManager> logger;
        private readonly Lock lockObject = new();
        private readonly IAppSettingsService settingsService;
        private readonly ICombatHistoryStore historyStore;
        private PlayerStatSnapshot? latestPlayerStatSnapshot;
        private readonly List<BuffEvent> activeBuffBacklog = new();


        public CombatSessionManager(
            EntityTracker entityTracker,
            ILoggerFactory loggerFactory,
            IAppSettingsService settingsService,
            ICombatHistoryStore historyStore)
        {
            this.entityTracker = entityTracker;
            this.settingsService = settingsService;
            this.historyStore = historyStore;
            targetResolver = new ActiveTargetResolver(entityTracker);
            logger = loggerFactory.CreateLogger<CombatSessionManager>();
            entityTracker.SummonRegistered += OnSummonRegistered;
        }


        public void FirePingUpdate(int pingMs)
        {
            PingUpdated?.Invoke(this, pingMs);
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
      
        public HistorySessionPageResult GetHistoryPage(HistorySessionQuery query)
        {
            lock (lockObject)
            {
                int pageNumber = Math.Max(1, query.PageNumber);
                int pageSize = Math.Clamp(query.PageSize, 1, 500);
                int globalSkip = (pageNumber - 1) * pageSize;

                var running = targetEntries.Values
                    .Select(e => e.CurrentSession)
                    .Where(s => s is not null && s is { IsCompleted: false, TargetInfo.IsBoss: true })
                    .Select(s => CreateListItem(s!))
                    .Where(s => IsMatch(s, query))
                    .OrderByDescending(x => x.SessionEnd)
                    .ToList();

                var runningIds = running.Select(x => x.SessionId).ToHashSet();
                int persistedCount = historyStore.GetSessionCount(
                    query.DateFrom,
                    query.DateTo,
                    query.BossNameContains,
                    runningIds);

                int totalCount = running.Count + persistedCount;

                int runningSkip = Math.Min(globalSkip, running.Count);
                int runningTake = Math.Min(pageSize, Math.Max(0, running.Count - runningSkip));
                var pageItems = running.Skip(runningSkip).Take(runningTake).ToList();

                int persistedTake = pageSize - pageItems.Count;
                if (persistedTake > 0)
                {
                    int persistedSkip = Math.Max(0, globalSkip - running.Count);
                    var persistedPage = historyStore.GetSessionPage(
                        query.DateFrom,
                        query.DateTo,
                        query.BossNameContains,
                        persistedSkip,
                        persistedTake,
                        runningIds);

                    pageItems.AddRange(persistedPage);
                }

                return new HistorySessionPageResult
                {
                    Items = pageItems,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                };
            }
        }

        public HistorySessionSnapshot? GetHistorySession(Guid sessionId)
        {
            lock (lockObject)
            {
                var live = targetEntries.Values
                    .SelectMany(e => e.AllSessions)
                    .FirstOrDefault(s => s.SessionId == sessionId);

                if (live is not null)
                    return HistorySessionSnapshot.From(live);

                return historyStore.GetSession(sessionId);
            }
        }

        public IReadOnlyCollection<PlayerStats> PlayerStats
        {
            get { lock (lockObject) { return GetActiveTargetSession()?.GetPlayerStats() ?? []; } }
        }

        public PlayerStatSnapshot? GetCurrentPlayerStatSnapshot()
        {
            lock (lockObject)
            {
                return latestPlayerStatSnapshot;
            }
        }

        public void ProcessPlayerStatsUpdate(PlayerCharacterStats stats, DateTime? capturedAt = null)
        {
            ArgumentNullException.ThrowIfNull(stats);

            lock (lockObject)
            {
                latestPlayerStatSnapshot = PlayerStatSnapshot.Create(stats, capturedAt);
            }
        }

        public void RegisterPlayerDeath(int playerId)
        {
            GetActiveTargetSession()?.RegisterPlayerDeath(playerId);
        }

        public double GetPartyDps()
        {
            double totalDamage = PlayerStats.Sum(r => r.TotalDamage);
            var combatDuration = GetCombatDuration();
            return totalDamage / combatDuration.TotalSeconds;
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

                var buffs = session.GetBuffEvents(playerId, sessionStart, sessionEnd);
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
                var buffEvents   = session.GetBuffEvents(playerId, sessionStart, sessionEnd);

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
                    if (settingsService.BossOnlyCapture)
                    {
                        var mob = entityTracker.GetTargetMob(damageEvent.TargetEntity.Id) ?? damageEvent.TargetEntity;
                        if (!mob.IsBoss) return;
                    }

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
                    activeBuffBacklog.RemoveAll(b => b.AppliedAt.AddMilliseconds(b.DurationMs) <= buffEvent.AppliedAt);
                    activeBuffBacklog.Add(buffEvent);

                    foreach (var targetEntry in AllTargetEntries)
                    {
                        targetEntry.TryAddBuff(buffEvent);

                    }
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

        public void CompleteAndPersistActiveSessions(DateTime? at = null)
        {
            lock (lockObject)
            {
                var completedAt = at ?? DateTime.Now;
                foreach (var entry in targetEntries.Values)
                {
                    entry.CompleteActiveSession();
                }

                targetResolver.Update(targetEntries.Values, completedAt);
            }
        }

        private IReadOnlyCollection<TargetEntry> AllTargetEntries
        {
            get { lock (lockObject) { return targetEntries.Values.ToList(); } }
        }


        private void RouteToTargetEntry(PlayerDamage damageEvent)
        {
            var entry = targetEntries.GetOrAdd(
                damageEvent.TargetEntity.Id,
                id => new TargetEntry(id, entityTracker, settingsService, OnSessionCompleted));

            var previousSession = entry.CurrentSession;
            entry.AddDamage(damageEvent);
            var currentSession = entry.CurrentSession;

            if (!ReferenceEquals(previousSession, currentSession) && currentSession is not null)
            {
                ReplayPreCombatBuffs(currentSession);
            }
        }

        private void ReplayPreCombatBuffs(TargetCombatSession session)
        {
            var sessionStart = session.SessionStart;
            foreach (var buff in activeBuffBacklog)
            {
                if (buff.AppliedAt >= sessionStart)
                    continue;

                var buffEnd = buff.AppliedAt.AddMilliseconds(buff.DurationMs);
                if (buffEnd <= sessionStart)
                    continue;

                session.ProcessBuffEvent(buff);
            }
        }

        private void OnSessionCompleted(TargetCombatSession session)
        {
            try
            {
                historyStore.Save(HistorySessionSnapshot.From(session));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist completed combat session {SessionId}", session.SessionId);
            }
        }

        private static HistorySessionListItem CreateListItem(TargetCombatSession session)
        {
            var playerStats = session.GetPlayerStats();
            return new HistorySessionListItem
            {
                SessionId = session.SessionId,
                TargetId = session.TargetId,
                TargetName = session.TargetInfo.Name,
                TargetHpTotal = session.TargetInfo.HpTotal,
                SessionStart = session.SessionStart,
                SessionEnd = session.LastHitTime,
                State = session.State,
                TotalDamage = playerStats.Sum(p => p.TotalDamage),
                PlayerCount = playerStats.Count(p => p.IsIdentified || p.DamagePercentage > 1),
            };
        }

        private static bool IsMatch(HistorySessionListItem item, HistorySessionQuery query)
        {
            if (query.DateFrom is { } from && item.SessionEnd < from)
                return false;

            if (query.DateTo is { } to && item.SessionEnd > to)
                return false;

            if (!string.IsNullOrWhiteSpace(query.BossNameContains))
            {
                var search = query.BossNameContains.Trim();
                if (item.TargetName.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            return true;
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
            damageEvent.SourceSummon = new Player
            {
                CharacterClass = damageEvent.SourceEntity.CharacterClass,
                Icon = damageEvent.SourceEntity.Icon,
                Name = damageEvent.SourceEntity.Name,
                Id = damageEvent.SourceEntity.Id,
            };
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
            latestPlayerStatSnapshot = null;
            activeBuffBacklog.Clear();
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