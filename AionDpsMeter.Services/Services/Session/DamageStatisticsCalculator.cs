using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    public static class DamageStatisticsCalculator
    {
        private const double MinDurationSeconds = 0.1;

        public static PlayerStats ComputePlayerStats(PlayerSession session, long totalCombatDamage)
        {
            var hits = session.Hits.ToList();
            var nonDotHits = hits.Where(h => !h.IsDot).ToList();

            var duration = GetDuration(session);

            return new PlayerStats
            {
                PlayerId = session.PlayerId,
                PlayerName = session.PlayerName,
                IsIdentified = session.IsIdentified,
                PlayerIcon = session.PlayerIcon,
                ClassName = session.ClassName,
                ClassIcon = session.ClassIcon,
                IsUser = session.IsUser,
                CombatPower = session.CombatPower,
                ServerName = session.ServerName,
                TotalDamage = session.TotalDamage,

                HitCount = nonDotHits.Count,
                CriticalHits = nonDotHits.Count(h => h.IsCritical),
                BackAttacks = nonDotHits.Count(h => h.IsBackAttack),
                PerfectHits = nonDotHits.Count(h => h.IsPerfect),
                DoubleDamageHits = nonDotHits.Count(h => h.IsDoubleDamage),
                ParryHits = nonDotHits.Count(h => h.IsParry),

                DamagePerSecond = session.TotalDamage / duration,
                DamagePercentage = GetPercentage(session.TotalDamage, totalCombatDamage),

                FirstHit = session.FirstHit ?? default,
                LastHit = session.LastHit ?? default,
            };
        }

        public static IReadOnlyCollection<SkillStats> ComputeSkillStats(PlayerSession session, bool groupSummonDamage)
        {
            var duration = GetDuration(session);
            var hits = session.Hits.ToList();

            var regularHits = hits.Where(h => h.SourceSummon is null);
            var summonHits = hits.Where(h => h.SourceSummon is not null);

            var list1 = regularHits.Where(r => r.Skill.Name.Contains("Summon: Fire Spirit")).ToList();
            var list2 = summonHits.Where(r => r.Skill.Name.Contains("Summon: Fire Spirit")).ToList();

            var skillMap = regularHits
                .GroupBy(h => h.Skill.Id)
                .Select(g => CreateSkillStats(g, duration, session.TotalDamage))
                .ToDictionary(s => s.SkillId);

            if (!groupSummonDamage)
            {
                var skillMap2 = summonHits
                .GroupBy(h => h.Skill.Id)
                .Select(g => CreateSkillStats(g, duration, session.TotalDamage))
                .ToDictionary(s => s.SkillId);

                foreach (var kvp in skillMap2)
                    skillMap[kvp.Key] = kvp.Value;
            }
            else
            {

                //List<List<SkillStats>> xdd = new List<List<SkillStats>>();
                //foreach (var summonGroup in summonHits.GroupBy(h => h.SourceSummon!.Id))
                //{
                //    var skillStats = summonGroup
                //        .GroupBy(h => h.Skill.Id)
                //        .Select(g => CreateSkillStats(g, duration, session.TotalDamage))
                //        .ToList();
                //    xdd.Add(skillStats);
                //    MergeSummonGroup(skillStats, session.TotalDamage, duration, skillMap);
                //}

                var allSummonSkillStats = summonHits
                    .GroupBy(h => h.SourceSummon!.Id)
                    .Select(summonGroup => summonGroup
                        .GroupBy(h => h.Skill.Id)
                        .Select(g => CreateSkillStats(g, duration, session.TotalDamage))
                        .ToList())
                    .ToList();

                MergeAllSummonGroups(allSummonSkillStats, session.TotalDamage, duration, skillMap);
                Console.WriteLine();
            }

            return skillMap.Values
                .OrderByDescending(s => s.TotalDamage)
                .ToList();
        }

        private static void MergeAllSummonGroups(
     List<List<SkillStats>> allSummonSkillStats,
     long sessionTotalDamage,
     double duration,
     Dictionary<long, SkillStats> skillMap)
        {
            var buckets = new Dictionary<long, List<List<SkillStats>>>();

            foreach (var skillStats in allSummonSkillStats)
            {
                var ownerSkill = skillStats.FirstOrDefault(s => s.IsClassSkill);
                var bucketKey = ownerSkill?.SkillId ?? skillStats[0].SkillId;

                if (!buckets.TryGetValue(bucketKey, out var bucket))
                {
                    bucket = new List<List<SkillStats>>();
                    buckets[bucketKey] = bucket;
                }

                bucket.Add(skillStats);
            }

            foreach (var (_, bucket) in buckets)
            {
                // Flatten then re-group by SkillId, merging duplicates within each group
                var mergedBySkillId = bucket
                    .SelectMany(x => x)
                    .GroupBy(s => s.SkillId)
                    .Select(g => g.Count() == 1 ? g.First() : MergeSkillStats(g.ToList(), sessionTotalDamage, duration))
                    .ToList();

                MergeSummonGroup(mergedBySkillId, sessionTotalDamage, duration, skillMap);
            }
        }

        private static SkillStats MergeSkillStats(
            List<SkillStats> stats,
            long sessionTotalDamage,
            double duration)
        {
            var merged = (stats.FirstOrDefault(s => s.IsClassSkill) ?? stats[0]).Clone();

            merged.TotalDamage = stats.Sum(s => s.TotalDamage);
            merged.HitCount = stats.Sum(s => s.HitCount);
            merged.CriticalHits = stats.Sum(s => s.CriticalHits);
            merged.BackAttacks = stats.Sum(s => s.BackAttacks);
            merged.PerfectHits = stats.Sum(s => s.PerfectHits);
            merged.DoubleDamageHits = stats.Sum(s => s.DoubleDamageHits);
            merged.ParryHits = stats.Sum(s => s.ParryHits);
            merged.AdpcDamage = stats.Sum(s => s.AdpcDamage);
            merged.AdpcHitCount = stats.Sum(s => s.AdpcHitCount);

            merged.MinHit = stats.Min(s => s.MinHit);
            merged.MaxHit = stats.Max(s => s.MaxHit);

            merged.DamagePerSecond = merged.TotalDamage / duration;
            merged.DamagePercentage = GetPercentage(merged.TotalDamage, sessionTotalDamage);

            return merged;
        }

        private static void MergeSummonGroup(
            List<SkillStats> skillStats,
            long sessionTotalDamage,
            double duration,
            Dictionary<long, SkillStats> skillMap)
        {
            var ownerSkill = skillStats.FirstOrDefault(s => s.IsClassSkill);

            if (ownerSkill is null)
            {
                foreach (var stat in skillStats)
                    skillMap[stat.SkillId] = stat;

                return;
            }

            var merged = ownerSkill.Clone();

            merged.TotalDamage = skillStats.Sum(s => s.TotalDamage);
            merged.HitCount = skillStats.Sum(s => s.HitCount);
            merged.CriticalHits = skillStats.Sum(s => s.CriticalHits);
            merged.BackAttacks = skillStats.Sum(s => s.BackAttacks);
            merged.PerfectHits = skillStats.Sum(s => s.PerfectHits);
            merged.DoubleDamageHits = skillStats.Sum(s => s.DoubleDamageHits);
            merged.ParryHits = skillStats.Sum(s => s.ParryHits);
            merged.AdpcDamage = skillStats.Sum(s => s.AdpcDamage);
            merged.AdpcHitCount = skillStats.Sum(s => s.AdpcHitCount);

            merged.MinHit = skillStats.Min(s => s.MinHit);
            merged.MaxHit = skillStats.Max(s => s.MaxHit);

            merged.DamagePerSecond = merged.TotalDamage / duration;

            merged.DamagePercentage = GetPercentage(
                merged.TotalDamage,
                sessionTotalDamage);

            merged.SummonChildren = skillStats;
            merged.IsSummonGroup = skillStats.Count > 1;

            skillMap[merged.SkillId] = merged;
        }

        private static SkillStats CreateSkillStats(
            IGrouping<int, PlayerDamage> group,
            double duration,
            long sessionTotalDamage)
        {
            var nonDot = group.Where(h => !h.IsDot).ToList();
            var adpcHits = nonDot.Where(h => h is { IsCritical: true, IsDoubleDamage: true, IsPerfect: true }).ToList();
            var totalDamage = group.Sum(h => h.Damage);
            var first = group.First();

            return new SkillStats
            {
                SkillId = group.Key,
                SkillName = first.Skill.Name,
                SkillIcon = first.Skill.Icon,
                SpecializationFlags = first.Skill.SpecializationFlags,

                IsDot = group.All(h => h.IsDot),
                TotalDamage = totalDamage,
                HitCount = group.Count(),

                CriticalHits = nonDot.Count(h => h.IsCritical),
                BackAttacks = nonDot.Count(h => h.IsBackAttack),
                PerfectHits = nonDot.Count(h => h.IsPerfect),
                DoubleDamageHits = nonDot.Count(h => h.IsDoubleDamage),
                ParryHits = nonDot.Count(h => h.IsParry),
                AdpcDamage = adpcHits.Sum(h => h.Damage),
                AdpcHitCount = adpcHits.Count,

                MinHit = group.Min(h => h.Damage),
                MaxHit = group.Max(h => h.Damage),

                IsClassSkill = group.Any(r => r.CharacterClass.Id > 10),

                DamagePerSecond = totalDamage / duration,
                DamagePercentage = GetPercentage(totalDamage, sessionTotalDamage),
            };
        }

        private static double GetDuration(PlayerSession session)
        {
            if (session.FirstHit is null || session.LastHit is null)
                return MinDurationSeconds;

            return Math.Max(
                (session.LastHit.Value - session.FirstHit.Value).TotalSeconds,
                MinDurationSeconds);
        }

        private static double GetPercentage(long value, long total)
            => total > 0 ? (double)value / total * 100 : 0;
    }
}