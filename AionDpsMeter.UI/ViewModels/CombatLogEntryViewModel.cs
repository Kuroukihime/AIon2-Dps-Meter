using AionDpsMeter.Core.Models;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed class CombatLogEntryViewModel : ViewModelBase
    {
        private readonly PlayerDamage _damageEvent;

        public CombatLogEntryViewModel(PlayerDamage damageEvent)
        {
            _damageEvent = damageEvent;
        }

        public DateTime DateTime      => _damageEvent.DateTime;
        public string TimeFormatted   => _damageEvent.DateTime.ToString("HH:mm:ss.fff");
        public string SourceName      => _damageEvent.SourceEntity.Name;
        public string TargetName      => _damageEvent.TargetEntity.Name;
        public string SkillName       => _damageEvent.Skill.Name;
        public string? SkillIcon      => _damageEvent.Skill.Icon;
        public bool HasSkillIcon      => _damageEvent.Skill.HasIcon;
        public long Damage            => _damageEvent.Damage;
        public string DamageFormatted => DamageFormatter.Format(_damageEvent.Damage);
        public bool IsCritical        => _damageEvent.IsCritical;
        public bool IsBackAttack      => _damageEvent.IsBackAttack;
        public bool IsPerfect         => _damageEvent.IsPerfect;
        public bool IsDoubleDamage    => _damageEvent.IsDoubleDamage;
        public bool IsParry           => _damageEvent.IsParry;

        /// <summary>Delegates flag label aggregation to the domain model.</summary>
        public string Flags => _damageEvent.Flags;
    }
}
