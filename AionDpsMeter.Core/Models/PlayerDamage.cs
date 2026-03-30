namespace AionDpsMeter.Core.Models
{
    public sealed class PlayerDamage
    {
        public DateTime DateTime { get; init; }
        public Player SourceEntity { get; set; } = null!;
        public Mob TargetEntity { get; init; } = null!;
        public Skill Skill { get; init; } = null!;
        public CharacterClass CharacterClass { get; init; } = null!;
        public long Damage { get; set; }
        public bool IsCritical { get; init; }
        public bool IsBackAttack { get; init; }
        public bool IsPerfect { get; init; }
        public bool IsDoubleDamage { get; init; }
        public bool IsParry { get; init; }
        public long[]? PotentialDamageData { get; init; }

        /// <summary>
        /// Space-separated label string of all active hit flags (e.g. "CRIT BACK").
        /// Empty string when no flags are set.
        /// </summary>
        public string Flags
        {
            get
            {
                var flags = new List<string>(5);
                if (IsCritical)     flags.Add("CRIT");
                if (IsBackAttack)   flags.Add("BACK");
                if (IsPerfect)      flags.Add("PERFECT");
                if (IsDoubleDamage) flags.Add("x2");
                if (IsParry)        flags.Add("PARRY");
                return flags.Count > 0 ? string.Join(" ", flags) : string.Empty;
            }
        }
    }
}
