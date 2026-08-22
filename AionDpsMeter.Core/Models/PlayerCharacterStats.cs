using AionDpsMeter.Core.Data;

namespace AionDpsMeter.Services.Models
{
    public class PlayerCharacterStats
    {
        protected readonly IReadOnlyDictionary<ushort, int> Values;

        public PlayerCharacterStats(IReadOnlyDictionary<ushort, int> values)
        {
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>Raw integer value for a stat ID, or null if not present in the packet.</summary>
        protected int? Raw(ushort statId) => Values.TryGetValue(statId, out var value) ? value : null;

        /// <summary>Converts a basis-points value to a percentage, or null if absent.</summary>
        protected double? Percent(ushort statId) => Raw(statId) is int bp ? bp / 100.0 : null;

        // ---- Attack ---------------------------------------------------------

        public virtual int? AttackGear => Raw(StatIds.AttackGear);
        public virtual int? AttackBase => Raw(StatIds.AttackBase);
        public virtual int? AttackMax => Raw(StatIds.AttackMax);
        public virtual int? AttackMin => Raw(StatIds.AttackMin);
        public virtual double? AttackIncreasePercent => Percent(StatIds.AttackIncrease);
        public virtual int? AttackPve => Raw(StatIds.AttackPve);
        public virtual int? AttackBoss => Raw(StatIds.AttackBoss);


        // ---- Damage amplification -------------------------------------------

        public virtual double? DamageBoostPercent => Percent(StatIds.DamageBoost);
        public virtual double? WeaponDamageBoostPercent => Percent(StatIds.WeaponDamageBoost);
        public virtual double? PveDamageBoostPercent => Percent(StatIds.PveDamageBoost);
        public virtual double? BossDamageBoostPercent => Percent(StatIds.BossDamageBoost);
        public virtual double? CriticalDamageBoostPercent => Percent(StatIds.CriticalDamageBoost);
        public virtual double? PerfectPercent => Percent(StatIds.Perfect);
        public virtual double? HardHitPercent => Percent(StatIds.Smite);
        public virtual double? FrontDamageBoostPercent => Percent(StatIds.FrontDamageBoost);
        public virtual double? BackDamageBoostPercent => Percent(StatIds.BackDamageBoost);

 

        // ---- Damage-type amplification ----------------------------------------

        public virtual double? CongniDamageBoostPercent => Percent(StatIds.CongniDamageBoost);
        public virtual double? FeraDamageBoostPercent => Percent(StatIds.FeraDamageBoost);
        public virtual double? NatureDamageBoostPercent => Percent(StatIds.NatureDamageBoost);
        public virtual double? VarienDamageBoostPercent => Percent(StatIds.VarienDamageBoost);


        // ---- Mics ----------------------------------------
        public virtual double? CombatSpeedPercent => Percent(StatIds.CombatSpeed);
    }

}
