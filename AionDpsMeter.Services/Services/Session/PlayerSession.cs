using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class PlayerSession
    {
        public long PlayerId { get; }
        public string PlayerName { get; }
        public string? PlayerIcon { get; }
        public long ClassId { get; }
        public string ClassName { get; }
        public string? ClassIcon { get; }

        private readonly List<PlayerDamage> damageHistory = new();
        private readonly Dictionary<long, SkillStats> skillStats = new();
        private readonly ILogger<PlayerSession> logger;
        public PlayerStats Stats { get; private set; }

        public PlayerSession(PlayerDamage firstDamage, ILogger<PlayerSession> logger)
        {
            this.logger = logger;
            PlayerId = firstDamage.SourceEntity.Id;
            PlayerName = firstDamage.SourceEntity.Name;
            PlayerIcon = firstDamage.SourceEntity.Icon;
            ClassId = firstDamage.CharacterClass.Id;
            ClassName = firstDamage.CharacterClass.Name;
            ClassIcon = firstDamage.CharacterClass.Icon;
            Stats = new PlayerStats
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName,
                PlayerIcon = PlayerIcon,
                ClassName = ClassName,
                ClassIcon = ClassIcon,
                FirstHit = firstDamage.DateTime,
                LastHit = firstDamage.DateTime
            };
        }

        public void AddDamage(PlayerDamage damage) => damageHistory.Add(damage);

        public void CountRecentTargetHits(DateTime cutoff, Dictionary<int, int> targetCounts)
        {
            for (int i = damageHistory.Count - 1; i >= 0; i--)
            {
                var damage = damageHistory[i];
                if (damage.DateTime < cutoff) break;

                var targetId = damage.TargetEntity.Id;
                targetCounts.TryGetValue(targetId, out var count);
                targetCounts[targetId] = count + 1;
            }
        }

        public void UpdateStats(long totalCombatDamage, int activeTargetId)
        {
            long totalDamage = 0;
            int hitCount = 0, criticalHits = 0, backAttacks = 0, perfectHits = 0, doubleDamageHits = 0, parryHits = 0;
            DateTime firstHit = DateTime.MaxValue;
            DateTime lastHit = DateTime.MinValue;

            foreach (var r in damageHistory)
            {
                if (r.TargetEntity.Id != activeTargetId) continue;

                totalDamage += r.Damage;
                hitCount++;
                if (r.IsCritical) criticalHits++;
                if (r.IsBackAttack) backAttacks++;
                if (r.IsPerfect) perfectHits++;
                if (r.IsDoubleDamage) doubleDamageHits++;
                if (r.IsParry) parryHits++;
                if (r.DateTime < firstHit) firstHit = r.DateTime;
                if (r.DateTime > lastHit) lastHit = r.DateTime;
            }

            Stats.TotalDamage = totalDamage;
            Stats.HitCount = hitCount;
            Stats.CriticalHits = criticalHits;
            Stats.BackAttacks = backAttacks;
            Stats.PerfectHits = perfectHits;
            Stats.DoubleDamageHits = doubleDamageHits;
            Stats.ParryHits = parryHits;

            if (hitCount == 0)
            {
                Stats.DamagePerSecond = 0;
                Stats.DamagePercentage = 0;
                return;
            }

            Stats.FirstHit = firstHit;
            Stats.LastHit = lastHit;

            var playerDuration = (lastHit - firstHit).TotalSeconds;
            if (playerDuration < 0.1) playerDuration = 0.1;
            Stats.DamagePerSecond = totalDamage / playerDuration;
            Stats.DamagePercentage = totalCombatDamage > 0
                ? (double)totalDamage / totalCombatDamage * 100
                : 0;
        }

        public IReadOnlyList<PlayerDamage> GetDamageHistory(int activeTargetId)
        {
            var result = new List<PlayerDamage>();
            foreach (var damage in damageHistory)
            {
                if (damage.TargetEntity.Id == activeTargetId)
                    result.Add(damage);
            }
            return result;
        }

        public IReadOnlyCollection<SkillStats> GetSkillStats(int activeTargetId)
        {
            skillStats.Clear();

            foreach (var damage in damageHistory)
            {
                if (damage.TargetEntity.Id != activeTargetId) continue;

                if (!skillStats.TryGetValue(damage.Skill.Id, out var skill))
                {
                    skill = new SkillStats
                    {
                        SkillId = damage.Skill.Id,
                        SkillName = damage.Skill.Name,
                        SkillIcon = damage.Skill.Icon,
                        MinHit = long.MaxValue
                    };
                    skillStats[damage.Skill.Id] = skill;
                }

                skill.TotalDamage += damage.Damage;
                skill.HitCount++;
                if (damage.IsCritical) skill.CriticalHits++;
                if (damage.IsBackAttack) skill.BackAttacks++;
                if (damage.IsPerfect) skill.PerfectHits++;
                if (damage.IsDoubleDamage) skill.DoubleDamageHits++;
                if (damage.IsParry) skill.ParryHits++;
                if (damage.Damage < skill.MinHit) skill.MinHit = damage.Damage;
                if (damage.Damage > skill.MaxHit) skill.MaxHit = damage.Damage;
            }

            var playerDuration = (Stats.LastHit - Stats.FirstHit).TotalSeconds;
            if (playerDuration < 0.1) playerDuration = 0.1;

            foreach (var skill in skillStats.Values)
            {
                skill.DamagePercentage = Stats.TotalDamage > 0
                    ? (double)skill.TotalDamage / Stats.TotalDamage * 100
                    : 0;
                skill.DamagePerSecond = skill.TotalDamage / playerDuration;
            }

            var result = new List<SkillStats>(skillStats.Values);
            result.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));
            return result;
        }


        public void Reset()
        {
            damageHistory.Clear();
            skillStats.Clear();
            Stats = new PlayerStats
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName,
                PlayerIcon = PlayerIcon,
                ClassName = ClassName,
                ClassIcon = ClassIcon
            };
        }

    }
}