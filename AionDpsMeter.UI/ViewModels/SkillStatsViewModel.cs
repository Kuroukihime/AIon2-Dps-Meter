using AionDpsMeter.Services.Models;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed class SkillStatsViewModel : ViewModelBase
    {
        private readonly SkillStats _stats;

        public SkillStatsViewModel(SkillStats stats)
        {
            _stats = stats;
        }

        public long     SkillId            => _stats.SkillId;
        public string   SkillName          => (_stats.IsDot ? "[DOT] " : "")+ _stats.SkillName;
        public string?  SkillIcon          => _stats.SkillIcon;
        public bool     HasSkillIcon       => !string.IsNullOrEmpty(_stats.SkillIcon);
        public bool[]   SpecializationFlags => _stats.SpecializationFlags;

        // ── Damage ────────────────────────────────────────────────────────────
        public long   TotalDamage          => _stats.TotalDamage;
        public string TotalDamageFormatted => DamageFormatter.Format(_stats.TotalDamage);
        public double DamagePercentage     => _stats.DamagePercentage;
        public string DamagePercentageFormatted => DamageFormatter.FormatRate(_stats.DamagePercentage);
        public double DamagePerSecond      => _stats.DamagePerSecond;
        public string DpsFormatted         => DamageFormatter.Format(_stats.DamagePerSecond);

        // ── Hit stats ─────────────────────────────────────────────────────────
        public int    HitCount             => _stats.HitCount;
        public string HitCountFormatted    => _stats.HitCount.ToString();
        public int    CriticalHits         => _stats.CriticalHits;
        public int    BackAttacks          => _stats.BackAttacks;
        public int    PerfectHits          => _stats.PerfectHits;
        public int    DoubleDamageHits     => _stats.DoubleDamageHits;
        public int    ParryHits            => _stats.ParryHits;

        // ── Per-hit values ────────────────────────────────────────────────────
        public double AverageDamage        => _stats.AverageDamage;
        public string AverageDamageFormatted => DamageFormatter.Format(_stats.AverageDamage);
        public long   MaxHit               => _stats.MaxHit;
        public string MaxHitFormatted      => $"{_stats.MaxHit:N0}";
        /// <summary>Sentinel-safe minimum hit (0 when no hit has been recorded).</summary>
        public long   MinHit               => _stats.SafeMinHit;
        public string MinHitFormatted      => $"{_stats.SafeMinHit:N0}";

        // ── Rates ─────────────────────────────────────────────────────────────
        public double CriticalRate           => _stats.CriticalRate;
        public string CriticalRateFormatted  => DamageFormatter.FormatRateRounded(_stats.CriticalRate);
        public double BackAttackRate         => _stats.BackAttackRate;
        public string BackAttackRateFormatted => DamageFormatter.FormatRateRounded(_stats.BackAttackRate);
        public double PerfectRate            => _stats.PerfectRate;
        public string PerfectRateFormatted   => DamageFormatter.FormatRateRounded(_stats.PerfectRate);
        public double DoubleDamageRate       => _stats.DoubleDamageRate;
        public string DoubleDamageRateFormatted => DamageFormatter.FormatRateRounded(_stats.DoubleDamageRate);
        public double ParryRate              => _stats.ParryRate;
        public string ParryRateFormatted     => DamageFormatter.FormatRateRounded(_stats.ParryRate);
    }
}
