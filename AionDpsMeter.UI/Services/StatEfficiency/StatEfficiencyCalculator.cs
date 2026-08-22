namespace AionDpsMeter.UI.StatEfficiency
{
    public sealed class StatEfficiencyCalculator
    {
        public StatEfficiencyCalculationResult Calculate(
            StatEfficiencyStats baseStats,
            StatEfficiencyEnvironment environment,
            StatEfficiencyOptionDelta delta)
        {
            ArgumentNullException.ThrowIfNull(baseStats);
            ArgumentNullException.ThrowIfNull(environment);
            ArgumentNullException.ThrowIfNull(delta);

            var adjustedStats = baseStats.ApplyDelta(delta);

            double statWindowAttack = ComputeStatWindowAttack(baseStats, environment.AttackIncreaseCombatPercent);
            double effectiveAttack = ComputeEffectiveAttack(baseStats, environment.AttackIncreaseCombatPercent);
            double totalDamage = ComputeTotalDamage(baseStats, environment, effectiveAttack);

            double effectiveAttack2 = ComputeEffectiveAttack(adjustedStats, environment.AttackIncreaseCombatPercent);
            double totalDamage2 = ComputeTotalDamage(adjustedStats, environment, effectiveAttack2);

            double damageGainPercent = totalDamage <= 0
                ? 0
                : ((totalDamage2 / totalDamage) - 1) * 100;

            return new StatEfficiencyCalculationResult
            {
                StatWindowAttack = statWindowAttack,
                EffectiveAttack = effectiveAttack,
                TotalDamage = totalDamage,
                EffectiveAttack2 = effectiveAttack2,
                TotalDamage2 = totalDamage2,
                DamageGainPercent = damageGainPercent,
            };
        }

        private static double ComputeStatWindowAttack(StatEfficiencyStats stats, double attackIncreaseCombatPercent)
        {
            double avgAttack = (stats.MaxAttack + stats.MinAttack) / 2.0;
            return (stats.GearAttack + stats.BaseAttack + avgAttack)
                * (1 + PercentMath.ToRate(stats.AttackIncreasePercent + attackIncreaseCombatPercent));
        }

        private static double ComputeMinMaxPerfectAvg(StatEfficiencyStats stats)
        {
            double perfectRate = PercentMath.ToRate(PercentMath.Clamp01Percent(stats.PerfectPercent));
            double avgAttack = (stats.MaxAttack + stats.MinAttack) / 2.0;
            return perfectRate * stats.MaxAttack + (1 - perfectRate) * avgAttack;
        }

        private static double ComputeEffectiveAttack(StatEfficiencyStats stats, double attackIncreaseCombatPercent)
        {
            double weaponDamageRate = PercentMath.ToRate(stats.WeaponDamageBoostPercent);
            double minMaxPerfectAvg = ComputeMinMaxPerfectAvg(stats);
            double rawAttack = (stats.GearAttack * (1 + weaponDamageRate))
                + stats.BaseAttack
                + (minMaxPerfectAvg * (1 + weaponDamageRate));

            return rawAttack * (1 + PercentMath.ToRate(stats.AttackIncreasePercent + attackIncreaseCombatPercent))
                + stats.PveAttack
                + stats.BossAttack;
        }

        private static double ComputeTotalDamage(
            StatEfficiencyStats stats,
            StatEfficiencyEnvironment environment,
            double effectiveAttack)
        {
            double critMult = (150 + stats.CriticalDamageBoostPercent) / 100.0;
            double mCrit = 1 + PercentMath.ToRate(PercentMath.Clamp01Percent(environment.CritChance)) * (critMult - 1);

            double smiteProc = Math.Max(0,
                Math.Min(environment.PartySmiteBuff + stats.SmitePercent, 100)
                - environment.BossSmiteResist);
            double mSmite = 1 + PercentMath.ToRate(smiteProc);

            double mDirectional = environment.AttackType switch
            {
                AttackType.Back => 1 + PercentMath.ToRate(PercentMath.Clamp01Percent(environment.BackAttackRate))
                    * PercentMath.ToRate(stats.BackDamageBoostPercent),
                AttackType.Front => 1 + PercentMath.ToRate(PercentMath.Clamp01Percent(environment.FrontAttackRate))
                    * PercentMath.ToRate(stats.FrontDamageBoostPercent),
                _ => 1,
            };

            double amplificationPercent = stats.DamageBoostPercent
                + stats.PveDamageBoostPercent
                + stats.BossDamageBoostPercent
                + stats.RaceDamageBoostPercent
                + environment.PartyDamageBoost
                - environment.BossDamageTolerance;

            return effectiveAttack
                * (1 + PercentMath.ToRate(amplificationPercent))
                * mCrit
                * mSmite
                * mDirectional;
        }
    }
}
