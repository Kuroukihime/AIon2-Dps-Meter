using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    public static class DamageStatisticsCalculator
    {
        // DPS is damage over the whole fight's duration, so every row and skill adds up to the fight's total.
        public static PlayerStats ComputePlayerStats(PlayerSession session, long totalCombatDamage, double fightSeconds)
        {

            return new PlayerStats
            {
                PlayerId = session.PlayerId,
                PlayerName = session.PlayerName,
                IsIdentified = session.IsIdentified,
                PlayerIcon = session.PlayerIcon,
                ClassName = session.ClassName,
                ClassId = session.ClassId,
                ClassIcon = session.ClassIcon,
                IsUser = session.IsUser,
                CombatPower = session.CombatPower,
                ServerName = session.ServerName,
                PlayerDeaths = session.PlayerDeaths,
                TotalDamage = session.TotalDamage,

                HitCount = session.HitCount,
                CriticalHits = session.CriticalHits,
                BackAttacks = session.BackAttacks,
                FrontAttacks = session.FrontAttacks,
                PerfectHits = session.PerfectHits,
                DoubleDamageHits = session.DoubleDamageHits,
                ParryHits = session.ParryHits,

                DamagePerSecond = PerSecond(session.TotalDamage, fightSeconds),
                DamagePercentage = GetPercentage(session.TotalDamage, totalCombatDamage),

                FirstHit = session.FirstHit ?? default,
                LastHit = session.LastHit ?? default,
            };
        }

        public static IReadOnlyCollection<SkillStats> ComputeSkillStats(PlayerSession session, bool groupSummonDamage, double fightSeconds)
        {
            var duration = fightSeconds;
            var hits = session.Hits.ToList();

            var regularHitsList = hits.Where(h => h.SourceSummon is null).ToList();
            var summonHitsList = hits.Where(h => h.SourceSummon is not null).ToList();


            var regularSkillIds = regularHitsList
           .Select(h => h.Skill.Id)
           .ToHashSet();

            var summonHitsSharingId = summonHitsList
                .Where(h => regularSkillIds.Contains(h.Skill.Id))
                .ToList();

            var summonHitsRemaining = summonHitsList
                .Where(h => !regularSkillIds.Contains(h.Skill.Id))
                .ToList();

            regularHitsList.AddRange(summonHitsSharingId);
            summonHitsList = summonHitsRemaining;

            var regularHits = regularHitsList;
            var summonHits = summonHitsList;

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
                var allSummonSkillStats = summonHits
                    .GroupBy(h => h.SourceSummon!.Id)
                    .Select(summonGroup => summonGroup
                        .GroupBy(h => h.Skill.Id)
                        .Select(g => CreateSkillStats(g, duration, session.TotalDamage))
                        .ToList())
                    .ToList();

                MergeAllSummonGroups(allSummonSkillStats, session.TotalDamage, duration, skillMap);
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
            merged.FrontAttacks = stats.Sum(s => s.FrontAttacks);
            merged.PerfectHits = stats.Sum(s => s.PerfectHits);
            merged.DoubleDamageHits = stats.Sum(s => s.DoubleDamageHits);
            merged.ParryHits = stats.Sum(s => s.ParryHits);

            merged.MinHit = stats.Min(s => s.MinHit);
            merged.MaxHit = stats.Max(s => s.MaxHit);

            merged.DamagePerSecond = PerSecond(merged.TotalDamage, duration);
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
            merged.FrontAttacks = skillStats.Sum(s => s.FrontAttacks);
            merged.PerfectHits = skillStats.Sum(s => s.PerfectHits);
            merged.DoubleDamageHits = skillStats.Sum(s => s.DoubleDamageHits);
            merged.ParryHits = skillStats.Sum(s => s.ParryHits);

            merged.MinHit = skillStats.Min(s => s.MinHit);
            merged.MaxHit = skillStats.Max(s => s.MaxHit);

            merged.DamagePerSecond = PerSecond(merged.TotalDamage, duration);

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
                FrontAttacks = nonDot.Count(h => h.IsFrontAttack),
                PerfectHits = nonDot.Count(h => h.IsPerfect),
                DoubleDamageHits = nonDot.Count(h => h.IsDoubleDamage),
                ParryHits = nonDot.Count(h => h.IsParry),

                MinHit = group.Min(h => h.Damage),
                MaxHit = group.Max(h => h.Damage),

                IsClassSkill = group.Any(r => r.CharacterClass.Id > 10),

                DamagePerSecond = PerSecond(totalDamage, duration),
                DamagePercentage = GetPercentage(totalDamage, sessionTotalDamage),
            };
        }

        // Zero until the fight spans some time: the first hits often share one timestamp.
        private static double PerSecond(long damage, double seconds) => seconds > 0 ? damage / seconds : 0;

        private static double GetPercentage(long value, long total)
            => total > 0 ? (double)value / total * 100 : 0;
    }
}