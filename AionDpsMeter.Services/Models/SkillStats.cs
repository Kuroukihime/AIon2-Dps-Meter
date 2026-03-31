namespace AionDpsMeter.Services.Models
{
    public sealed class SkillStats
    {
        public long SkillId { get; init; }
        public string SkillName { get; init; } = string.Empty;
        public string? SkillIcon { get; init; }
        public bool[] SpecializationFlags { get; set; } = [];
        public long TotalDamage { get; set; }
        public int HitCount { get; set; }
        public int CriticalHits { get; set; }
        public int BackAttacks { get; set; }
        public int PerfectHits { get; set; }
        public int DoubleDamageHits { get; set; }
        public int ParryHits { get; set; }
        public long MaxHit { get; set; }
        /// <summary>Raw minimum hit. Initialised to <see cref="long.MaxValue"/> as a sentinel meaning "no hit yet".</summary>
        public long MinHit { get; set; } = long.MaxValue;
        public double DamagePerSecond { get; set; }
        public double DamagePercentage { get; set; }
        public bool IsDot { get; set; }

        // ── Derived stats ──────────────────────────────────────────────────────

        /// <summary>Minimum hit normalised for display: returns 0 when no hit has been recorded yet.</summary>
        public long SafeMinHit => MinHit == long.MaxValue ? 0 : MinHit;

        public double AverageDamage       => HitCount > 0 ? (double)TotalDamage / HitCount : 0;
        public double CriticalRate        => HitCount > 0 ? (double)CriticalHits    / HitCount * 100 : 0;
        public double BackAttackRate      => HitCount > 0 ? (double)BackAttacks     / HitCount * 100 : 0;
        public double PerfectRate         => HitCount > 0 ? (double)PerfectHits     / HitCount * 100 : 0;
        public double DoubleDamageRate    => HitCount > 0 ? (double)DoubleDamageHits / HitCount * 100 : 0;
        public double ParryRate           => HitCount > 0 ? (double)ParryHits       / HitCount * 100 : 0;
    }
}
