using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace AionDpsMeter.Services.Services.Session.Persistence
{
    public sealed class CombatHistoryStore : ICombatHistoryStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IDbContextFactory<CombatHistoryDbContext> _dbContextFactory;
        private readonly ILogger<CombatHistoryStore> _logger;
        private readonly IAppSettingsService _settingsService;
        private readonly ConcurrentQueue<CombatSessionEntity> _pendingSaves = new();
        private readonly Lock _saveDrainLock = new();
        private int _saveWorkerStarted;

        public CombatHistoryStore(
            IDbContextFactory<CombatHistoryDbContext> dbContextFactory,
            ILogger<CombatHistoryStore> logger,
            IAppSettingsService settingsService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _settingsService = settingsService;
            using var db = _dbContextFactory.CreateDbContext();
            db.Database.EnsureCreated();
            _ = Task.Run(CleanupExpiredSessionsSafe);
        }

        public void Save(HistorySessionSnapshot snapshot)
        {
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
                PlayerStatsJson = SerializeToJson(snapshot.PlayerStats),
                SkillStatsByPlayerJson = SerializeToJson(snapshot.SkillStatsByPlayer),
                BuffStatsByPlayerJson = SerializeToJson(snapshot.BuffStatsByPlayer),
                HitsByPlayerJson = SerializeToJson(snapshot.HitsByPlayer),
                BuffEventsByPlayerJson = SerializeToJson(snapshot.BuffEventsByPlayer),
            };

            _pendingSaves.Enqueue(entity);
            StartSaveWorkerIfNeeded();
        }

        public void FlushPendingSaves()
        {
            DrainPendingSaves();
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

        public int GetSessionCount(DateTime? dateFrom, DateTime? dateTo, string? bossNameContains, IReadOnlySet<Guid>? excludeSessionIds = null)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return BuildFilteredQuery(db, dateFrom, dateTo, bossNameContains, excludeSessionIds).Count();
        }

        public IReadOnlyList<HistorySessionListItem> GetSessionPage(
            DateTime? dateFrom,
            DateTime? dateTo,
            string? bossNameContains,
            int skip,
            int take,
            IReadOnlySet<Guid>? excludeSessionIds = null)
        {
            using var db = _dbContextFactory.CreateDbContext();

            return BuildFilteredQuery(db, dateFrom, dateTo, bossNameContains, excludeSessionIds)
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
                var decompressedJson = DecompressIfNeeded(json);
                return JsonSerializer.Deserialize<T>(decompressedJson, JsonOptions);
            }
            catch
            {
                return default;
            }
        }

        private static string SerializeToJson<T>(T value)
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }

        private static string CompressJson(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);

            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            {
                brotli.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(output.ToArray());
        }

        private static string DecompressIfNeeded(string value)
        {
            try
            {
                var compressedBytes = Convert.FromBase64String(value);
                using var input = new MemoryStream(compressedBytes);
                using var brotli = new BrotliStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();

                brotli.CopyTo(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
            catch (FormatException)
            {
                return value;
            }
            catch (InvalidDataException)
            {
                return value;
            }
        }

        private static IQueryable<CombatSessionEntity> BuildFilteredQuery(
            CombatHistoryDbContext db,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? bossNameContains,
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

            if (excludeSessionIds is { Count: > 0 })
                query = query.Where(x => !excludeSessionIds.Contains(x.SessionId));

            return query;
        }

        private void CleanupExpiredSessions(CombatHistoryDbContext db)
        {
            var retentionDays = Math.Clamp(_settingsService.HistoryRetantionPeriod, 1, 9999);
            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var stopwatch = Stopwatch.StartNew();

            int deletedRows = db.Sessions
                .Where(x => x.SessionEnd < cutoff)
                .ExecuteDelete();

            db.Database.ExecuteSqlRaw("VACUUM;");
            db.Database.ExecuteSqlRaw("ANALYZE;");
            db.Database.ExecuteSqlRaw("PRAGMA optimize;");

            stopwatch.Stop();
            _logger.LogInformation(
                "Combat history startup cleanup completed. RetentionDays={RetentionDays}, Cutoff={Cutoff}, DeletedRows={DeletedRows}, ElapsedMs={ElapsedMs}",
                retentionDays,
                cutoff,
                deletedRows,
                stopwatch.Elapsed.TotalMilliseconds);
        }

        private void StartSaveWorkerIfNeeded()
        {
            if (Interlocked.CompareExchange(ref _saveWorkerStarted, 1, 0) != 0)
                return;

            _ = Task.Run(() =>
            {
                try
                {
                    DrainPendingSaves();
                }
                finally
                {
                    Interlocked.Exchange(ref _saveWorkerStarted, 0);
                    if (!_pendingSaves.IsEmpty)
                    {
                        StartSaveWorkerIfNeeded();
                    }
                }
            });
        }

        private void DrainPendingSaves()
        {
            lock (_saveDrainLock)
            {
                while (_pendingSaves.TryDequeue(out var entity))
                {
                    PersistEntity(entity);
                }
            }
        }

        private void PersistEntity(CombatSessionEntity entity)
        {
            var stopwatch = Stopwatch.StartNew();
            using var db = _dbContextFactory.CreateDbContext();

            entity.PlayerStatsJson = CompressJson(entity.PlayerStatsJson);
            entity.SkillStatsByPlayerJson = CompressJson(entity.SkillStatsByPlayerJson);
            entity.BuffStatsByPlayerJson = CompressJson(entity.BuffStatsByPlayerJson);
            entity.HitsByPlayerJson = CompressJson(entity.HitsByPlayerJson);
            entity.BuffEventsByPlayerJson = CompressJson(entity.BuffEventsByPlayerJson);

            db.Sessions.Upsert(entity);
            db.SaveChanges();

            stopwatch.Stop();
            _logger.LogInformation(
                "Combat session persisted. SessionId={SessionId}, TargetId={TargetId}, TotalDamage={TotalDamage}, ElapsedMs={ElapsedMs}",
                entity.SessionId,
                entity.TargetId,
                entity.TotalDamage,
                stopwatch.Elapsed.TotalMilliseconds);
        }

        private void CleanupExpiredSessionsSafe()
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                CleanupExpiredSessions(db);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Combat history startup cleanup failed.");
            }
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
