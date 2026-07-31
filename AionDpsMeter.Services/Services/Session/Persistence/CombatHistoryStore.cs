using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AionDpsMeter.Services.Services.Session.Persistence
{
    public sealed class CombatHistoryStore : ICombatHistoryStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IDbContextFactory<CombatHistoryDbContext> _dbContextFactory;

        public CombatHistoryStore(IDbContextFactory<CombatHistoryDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            using var db = _dbContextFactory.CreateDbContext();
            db.Database.EnsureCreated();
        }

        public void Save(HistorySessionSnapshot snapshot)
        {
            using var db = _dbContextFactory.CreateDbContext();

            var entity = new CombatSessionEntity
            {
                SessionId = snapshot.SessionId,
                TargetId = snapshot.TargetId,
                TargetName = snapshot.TargetName,
                TargetHpTotal = snapshot.TargetHpTotal,
                SessionStart = snapshot.SessionStart,
                SessionEnd = snapshot.SessionEnd,
                State = snapshot.State,
                TotalDamage = snapshot.PlayerStats.Sum(p => p.TotalDamage),
                PlayerCount = snapshot.PlayerStats.Count(p => p.IsIdentified || p.DamagePercentage > 1),
                PlayerStatsJson = JsonSerializer.Serialize(snapshot.PlayerStats, JsonOptions),
                SkillStatsByPlayerJson = JsonSerializer.Serialize(snapshot.SkillStatsByPlayer, JsonOptions),
                BuffStatsByPlayerJson = JsonSerializer.Serialize(snapshot.BuffStatsByPlayer, JsonOptions),
                HitsByPlayerJson = JsonSerializer.Serialize(snapshot.HitsByPlayer, JsonOptions),
                BuffEventsByPlayerJson = JsonSerializer.Serialize(snapshot.BuffEventsByPlayer, JsonOptions),
            };

            db.Sessions.Upsert(entity);
            db.SaveChanges();
        }

        public IReadOnlyList<HistorySessionListItem> GetSessionList()
        {
            using var db = _dbContextFactory.CreateDbContext();

            return db.Sessions
                .AsNoTracking()
                .OrderByDescending(x => x.SessionEnd)
                .Select(x => new HistorySessionListItem
                {
                    SessionId = x.SessionId,
                    TargetId = x.TargetId,
                    TargetName = x.TargetName,
                    TargetHpTotal = x.TargetHpTotal,
                    SessionStart = x.SessionStart,
                    SessionEnd = x.SessionEnd,
                    State = x.State,
                    TotalDamage = x.TotalDamage,
                    PlayerCount = x.PlayerCount,
                })
                .ToList();
        }

        public int GetSessionCount(DateTime? dateFrom, DateTime? dateTo, string? bossNameContains, long minTotalDamage, IReadOnlySet<Guid>? excludeSessionIds = null)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return BuildFilteredQuery(db, dateFrom, dateTo, bossNameContains, minTotalDamage, excludeSessionIds).Count();
        }

        public IReadOnlyList<HistorySessionListItem> GetSessionPage(
            DateTime? dateFrom,
            DateTime? dateTo,
            string? bossNameContains,
            long minTotalDamage,
            int skip,
            int take,
            IReadOnlySet<Guid>? excludeSessionIds = null)
        {
            using var db = _dbContextFactory.CreateDbContext();

            return BuildFilteredQuery(db, dateFrom, dateTo, bossNameContains, minTotalDamage, excludeSessionIds)
                .OrderByDescending(x => x.SessionEnd)
                .Skip(Math.Max(0, skip))
                .Take(Math.Max(0, take))
                .Select(x => new HistorySessionListItem
                {
                    SessionId = x.SessionId,
                    TargetId = x.TargetId,
                    TargetName = x.TargetName,
                    TargetHpTotal = x.TargetHpTotal,
                    SessionStart = x.SessionStart,
                    SessionEnd = x.SessionEnd,
                    State = x.State,
                    TotalDamage = x.TotalDamage,
                    PlayerCount = x.PlayerCount,
                })
                .ToList();
        }

        public HistorySessionSnapshot? GetSession(Guid sessionId)
        {
            using var db = _dbContextFactory.CreateDbContext();

            var entity = db.Sessions.AsNoTracking().FirstOrDefault(x => x.SessionId == sessionId);
            if (entity is null) return null;

            var playerStats = DeserializeOrDefault<List<PlayerStats>>(entity.PlayerStatsJson) ?? [];
            var skillStats = DeserializeOrDefault<Dictionary<long, IReadOnlyCollection<SkillStats>>>(entity.SkillStatsByPlayerJson)
                ?? new Dictionary<long, IReadOnlyCollection<SkillStats>>();
            var buffStats = DeserializeOrDefault<Dictionary<long, IReadOnlyCollection<BuffStats>>>(entity.BuffStatsByPlayerJson)
                ?? new Dictionary<long, IReadOnlyCollection<BuffStats>>();
            var hitsByPlayer = DeserializeOrDefault<Dictionary<long, IReadOnlyList<PlayerDamage>>>(entity.HitsByPlayerJson)
                ?? new Dictionary<long, IReadOnlyList<PlayerDamage>>();
            var buffEventsByPlayer = DeserializeOrDefault<Dictionary<long, IReadOnlyList<BuffEvent>>>(entity.BuffEventsByPlayerJson)
                ?? new Dictionary<long, IReadOnlyList<BuffEvent>>();

            return new HistorySessionSnapshot
            {
                SessionId = entity.SessionId,
                TargetId = entity.TargetId,
                TargetName = entity.TargetName,
                TargetHpTotal = entity.TargetHpTotal,
                SessionStart = entity.SessionStart,
                SessionEnd = entity.SessionEnd,
                State = entity.State,
                PlayerStats = playerStats,
                SkillStatsByPlayer = skillStats,
                BuffStatsByPlayer = buffStats,
                HitsByPlayer = hitsByPlayer,
                BuffEventsByPlayer = buffEventsByPlayer,
            };
        }

        private static T? DeserializeOrDefault<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            try
            {
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch
            {
                return default;
            }
        }

        private static IQueryable<CombatSessionEntity> BuildFilteredQuery(
            CombatHistoryDbContext db,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? bossNameContains,
            long minTotalDamage,
            IReadOnlySet<Guid>? excludeSessionIds)
        {
            var query = db.Sessions.AsNoTracking().AsQueryable();

            if (dateFrom is { } from)
                query = query.Where(x => x.SessionEnd >= from);

            if (dateTo is { } to)
                query = query.Where(x => x.SessionEnd <= to);

            if (!string.IsNullOrWhiteSpace(bossNameContains))
            {
                var search = bossNameContains.Trim().ToLower();
                query = query.Where(x => x.TargetName.ToLower().Contains(search));
            }

            query = query.Where(x => x.TotalDamage >= minTotalDamage);

            if (excludeSessionIds is { Count: > 0 })
                query = query.Where(x => !excludeSessionIds.Contains(x.SessionId));

            return query;
        }
    }

    internal static class DbSetUpsertExtensions
    {
        public static void Upsert(this DbSet<CombatSessionEntity> set, CombatSessionEntity entity)
        {
            var existing = set.Find(entity.SessionId);
            if (existing is null)
            {
                set.Add(entity);
                return;
            }

            existing.TargetId = entity.TargetId;
            existing.TargetName = entity.TargetName;
            existing.TargetHpTotal = entity.TargetHpTotal;
            existing.SessionStart = entity.SessionStart;
            existing.SessionEnd = entity.SessionEnd;
            existing.State = entity.State;
            existing.TotalDamage = entity.TotalDamage;
            existing.PlayerCount = entity.PlayerCount;
            existing.PlayerStatsJson = entity.PlayerStatsJson;
            existing.SkillStatsByPlayerJson = entity.SkillStatsByPlayerJson;
            existing.BuffStatsByPlayerJson = entity.BuffStatsByPlayerJson;
            existing.HitsByPlayerJson = entity.HitsByPlayerJson;
            existing.BuffEventsByPlayerJson = entity.BuffEventsByPlayerJson;
        }
    }
}
