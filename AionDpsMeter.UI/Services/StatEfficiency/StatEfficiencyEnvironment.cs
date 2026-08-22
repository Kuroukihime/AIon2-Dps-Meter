namespace AionDpsMeter.UI.StatEfficiency
{
    public sealed class StatEfficiencyEnvironment
    {
        public double CritChance { get; init; }
        public double BackAttackRate { get; init; }
        public double FrontAttackRate { get; init; }
        public AttackType AttackType { get; init; }

        public double AttackIncreaseCombatPercent { get; init; }
        public double PartyDamageBoost { get; init; }
        public double BossDamageTolerance { get; init; }
        public double PartySmiteBuff { get; init; }
        public double BossSmiteResist { get; init; }
    }
}
