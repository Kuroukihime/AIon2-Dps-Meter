namespace AionDpsMeter.UI.StatEfficiency
{
    public sealed class StatEfficiencyStats
    {
        public double BaseAttack { get; init; }
        public double GearAttack { get; init; }
        public double MinAttack { get; init; }
        public double MaxAttack { get; init; }
        public double PveAttack { get; init; }
        public double BossAttack { get; init; }

        public double AttackIncreasePercent { get; init; }

        public double DamageBoostPercent { get; init; }
        public double WeaponDamageBoostPercent { get; init; }
        public double PveDamageBoostPercent { get; init; }
        public double BossDamageBoostPercent { get; init; }
        public double CriticalDamageBoostPercent { get; init; }
        public double PerfectPercent { get; init; }
        public double SmitePercent { get; init; }
        public double FrontDamageBoostPercent { get; init; }
        public double BackDamageBoostPercent { get; init; }
        public double RaceDamageBoostPercent { get; init; }

        public double CombatSpeedPercent { get; init; }

        public StatEfficiencyStats ApplyDelta(StatEfficiencyOptionDelta delta)
        {
            return new StatEfficiencyStats
            {
                BaseAttack = BaseAttack + delta.BaseAttack,
                GearAttack = GearAttack + delta.GearAttack,
                MinAttack = MinAttack + delta.MinAttack,
                MaxAttack = MaxAttack,
                PveAttack = PveAttack + delta.PveAttack,
                BossAttack = BossAttack + delta.BossAttack,
                AttackIncreasePercent = AttackIncreasePercent + delta.AttackIncreasePercent,
                DamageBoostPercent = DamageBoostPercent + delta.DamageBoostPercent,
                WeaponDamageBoostPercent = WeaponDamageBoostPercent + delta.WeaponDamageBoostPercent,
                PveDamageBoostPercent = PveDamageBoostPercent,
                BossDamageBoostPercent = BossDamageBoostPercent,
                CriticalDamageBoostPercent = CriticalDamageBoostPercent + delta.CriticalDamageBoostPercent,
                PerfectPercent = PerfectPercent + delta.PerfectPercent,
                SmitePercent = SmitePercent + delta.SmitePercent,
                FrontDamageBoostPercent = FrontDamageBoostPercent + delta.FrontDamageBoostPercent,
                BackDamageBoostPercent = BackDamageBoostPercent + delta.BackDamageBoostPercent,
                RaceDamageBoostPercent = RaceDamageBoostPercent,
                CombatSpeedPercent = CombatSpeedPercent,
            };
        }
    }
}
