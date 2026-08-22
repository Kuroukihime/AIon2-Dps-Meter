using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.StatEfficiency;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed class StatEfficiencyCalculatorViewModel : ViewModelBase
    {
        private readonly IAppSettingsService _settingsService;
        private readonly StatEfficiencyCalculator _calculator = new();
        private bool _isBulkUpdating;

        private double _baseAttack;
        private double _gearAttack;
        private double _minAttack;
        private double _maxAttack;
        private double _pveAttack;
        private double _bossAttack;
        private double _attackIncreasePercent;

        private double _damageBoostPercent;
        private double _weaponDamageBoostPercent;
        private double _pveDamageBoostPercent;
        private double _bossDamageBoostPercent;
        private double _criticalDamageBoostPercent;
        private double _perfectPercent;
        private double _smitePercent;
        private double _frontDamageBoostPercent;
        private double _backDamageBoostPercent;
        private double _raceDamageBoostPercent;
        private double _combatSpeedPercent;

        private double _critChance;
        private double _backAttackRate;
        private double _frontAttackRate;
        private AttackType _selectedAttackType;

        private double _attackIncreaseCombatPercent;
        private double _partyDamageBoost;
        private double _bossDamageTolerance;
        private double _partySmiteBuff;
        private double _bossSmiteResist;

        private double _optionBaseAttack;
        private double _optionGearAttack;
        private double _optionMinAttack;
        private double _optionPveAttack;
        private double _optionBossAttack;
        private double _optionAttackIncreasePercent;
        private double _optionDamageBoostPercent;
        private double _optionWeaponDamageBoostPercent;
        private double _optionCriticalDamageBoostPercent;
        private double _optionPerfectPercent;
        private double _optionSmitePercent;
        private double _optionFrontDamageBoostPercent;
        private double _optionBackDamageBoostPercent;

        private double _statWindowAttack;
        private double _effectiveAttack;
        private double _totalDamage;
        private double _effectiveAttack2;
        private double _totalDamage2;
        private double _damageGainPercent;

        public StatEfficiencyCalculatorViewModel(IAppSettingsService settingsService)
        {
            _settingsService = settingsService;

            _critChance = PercentMath.ClampNonNegative(_settingsService.StatCalcCritChance);
            _backAttackRate = PercentMath.ClampNonNegative(_settingsService.StatCalcBackAttackRate);
            _frontAttackRate = PercentMath.ClampNonNegative(_settingsService.StatCalcFrontAttackRate);
            _selectedAttackType = ParseAttackType(_settingsService.StatCalcAttackType);

            _attackIncreaseCombatPercent = PercentMath.ClampNonNegative(_settingsService.StatCalcAttackIncreaseCombatPercent);
            _partyDamageBoost = PercentMath.ClampNonNegative(_settingsService.StatCalcPartyDamageBoost);
            _bossDamageTolerance = PercentMath.ClampNonNegative(_settingsService.StatCalcBossDamageTolerance);
            _partySmiteBuff = PercentMath.ClampNonNegative(_settingsService.StatCalcPartySmiteBuff);
            _bossSmiteResist = PercentMath.ClampNonNegative(_settingsService.StatCalcBossSmiteResist);

            Recalculate();
        }

        public double BaseAttack { get => _baseAttack; set => SetMainStat(ref _baseAttack, value); }
        public double GearAttack { get => _gearAttack; set => SetMainStat(ref _gearAttack, value); }
        public double MinAttack { get => _minAttack; set => SetMainStat(ref _minAttack, value); }
        public double MaxAttack { get => _maxAttack; set => SetMainStat(ref _maxAttack, value); }
        public double PveAttack { get => _pveAttack; set => SetMainStat(ref _pveAttack, value); }
        public double BossAttack { get => _bossAttack; set => SetMainStat(ref _bossAttack, value); }
        public double AttackIncreasePercent { get => _attackIncreasePercent; set => SetMainStat(ref _attackIncreasePercent, value); }

        public double DamageBoostPercent { get => _damageBoostPercent; set => SetMainStat(ref _damageBoostPercent, value); }
        public double WeaponDamageBoostPercent { get => _weaponDamageBoostPercent; set => SetMainStat(ref _weaponDamageBoostPercent, value); }
        public double PveDamageBoostPercent { get => _pveDamageBoostPercent; set => SetMainStat(ref _pveDamageBoostPercent, value); }
        public double BossDamageBoostPercent { get => _bossDamageBoostPercent; set => SetMainStat(ref _bossDamageBoostPercent, value); }
        public double CriticalDamageBoostPercent { get => _criticalDamageBoostPercent; set => SetMainStat(ref _criticalDamageBoostPercent, value); }
        public double PerfectPercent { get => _perfectPercent; set => SetMainStat(ref _perfectPercent, value); }
        public double SmitePercent { get => _smitePercent; set => SetMainStat(ref _smitePercent, value); }
        public double FrontDamageBoostPercent { get => _frontDamageBoostPercent; set => SetMainStat(ref _frontDamageBoostPercent, value); }
        public double BackDamageBoostPercent { get => _backDamageBoostPercent; set => SetMainStat(ref _backDamageBoostPercent, value); }
        public double RaceDamageBoostPercent { get => _raceDamageBoostPercent; set => SetMainStat(ref _raceDamageBoostPercent, value); }

        public double CombatSpeedPercent { get => _combatSpeedPercent; set => SetMainStat(ref _combatSpeedPercent, value); }

        public double CritChance
        {
            get => _critChance;
            set => SetEnvironment(ref _critChance, value, v => _settingsService.StatCalcCritChance = v);
        }

        public double BackAttackRate
        {
            get => _backAttackRate;
            set => SetEnvironment(ref _backAttackRate, value, v => _settingsService.StatCalcBackAttackRate = v);
        }

        public double FrontAttackRate
        {
            get => _frontAttackRate;
            set => SetEnvironment(ref _frontAttackRate, value, v => _settingsService.StatCalcFrontAttackRate = v);
        }

        public AttackType SelectedAttackType
        {
            get => _selectedAttackType;
            set
            {
                if (SetProperty(ref _selectedAttackType, value))
                {
                    _settingsService.StatCalcAttackType = value.ToString();
                    Recalculate();
                    OnPropertyChanged(nameof(IsAttackTypeNone));
                    OnPropertyChanged(nameof(IsAttackTypeFront));
                    OnPropertyChanged(nameof(IsAttackTypeBack));
                }
            }
        }

        public bool IsAttackTypeNone
        {
            get => SelectedAttackType == AttackType.None;
            set { if (value) SelectedAttackType = AttackType.None; }
        }

        public bool IsAttackTypeFront
        {
            get => SelectedAttackType == AttackType.Front;
            set { if (value) SelectedAttackType = AttackType.Front; }
        }

        public bool IsAttackTypeBack
        {
            get => SelectedAttackType == AttackType.Back;
            set { if (value) SelectedAttackType = AttackType.Back; }
        }

        public double AttackIncreaseCombatPercent
        {
            get => _attackIncreaseCombatPercent;
            set => SetEnvironment(ref _attackIncreaseCombatPercent, value, v => _settingsService.StatCalcAttackIncreaseCombatPercent = v);
        }

        public double PartyDamageBoost
        {
            get => _partyDamageBoost;
            set => SetEnvironment(ref _partyDamageBoost, value, v => _settingsService.StatCalcPartyDamageBoost = v);
        }

        public double BossDamageTolerance
        {
            get => _bossDamageTolerance;
            set => SetEnvironment(ref _bossDamageTolerance, value, v => _settingsService.StatCalcBossDamageTolerance = v);
        }

        public double PartySmiteBuff
        {
            get => _partySmiteBuff;
            set => SetEnvironment(ref _partySmiteBuff, value, v => _settingsService.StatCalcPartySmiteBuff = v);
        }

        public double BossSmiteResist
        {
            get => _bossSmiteResist;
            set => SetEnvironment(ref _bossSmiteResist, value, v => _settingsService.StatCalcBossSmiteResist = v);
        }

        public double OptionBaseAttack { get => _optionBaseAttack; set => SetOption(ref _optionBaseAttack, value); }
        public double OptionGearAttack { get => _optionGearAttack; set => SetOption(ref _optionGearAttack, value); }
        public double OptionMinAttack { get => _optionMinAttack; set => SetOption(ref _optionMinAttack, value); }
        public double OptionPveAttack { get => _optionPveAttack; set => SetOption(ref _optionPveAttack, value); }
        public double OptionBossAttack { get => _optionBossAttack; set => SetOption(ref _optionBossAttack, value); }
        public double OptionAttackIncreasePercent { get => _optionAttackIncreasePercent; set => SetOption(ref _optionAttackIncreasePercent, value); }
        public double OptionDamageBoostPercent { get => _optionDamageBoostPercent; set => SetOption(ref _optionDamageBoostPercent, value); }
        public double OptionWeaponDamageBoostPercent { get => _optionWeaponDamageBoostPercent; set => SetOption(ref _optionWeaponDamageBoostPercent, value); }
        public double OptionCriticalDamageBoostPercent { get => _optionCriticalDamageBoostPercent; set => SetOption(ref _optionCriticalDamageBoostPercent, value); }
        public double OptionPerfectPercent { get => _optionPerfectPercent; set => SetOption(ref _optionPerfectPercent, value); }
        public double OptionSmitePercent { get => _optionSmitePercent; set => SetOption(ref _optionSmitePercent, value); }
        public double OptionFrontDamageBoostPercent { get => _optionFrontDamageBoostPercent; set => SetOption(ref _optionFrontDamageBoostPercent, value); }
        public double OptionBackDamageBoostPercent { get => _optionBackDamageBoostPercent; set => SetOption(ref _optionBackDamageBoostPercent, value); }

        public double StatWindowAttack
        {
            get => _statWindowAttack;
            private set => SetProperty(ref _statWindowAttack, value);
        }

        public double EffectiveAttack
        {
            get => _effectiveAttack;
            private set
            {
                if (SetProperty(ref _effectiveAttack, value))
                    OnPropertyChanged(nameof(EffectiveAttackPowerChangeText));
            }
        }

        public double TotalDamage
        {
            get => _totalDamage;
            private set => SetProperty(ref _totalDamage, value);
        }

        public double EffectiveAttack2
        {
            get => _effectiveAttack2;
            private set
            {
                if (SetProperty(ref _effectiveAttack2, value))
                    OnPropertyChanged(nameof(EffectiveAttackPowerChangeText));
            }
        }

        public double TotalDamage2
        {
            get => _totalDamage2;
            private set => SetProperty(ref _totalDamage2, value);
        }

        public double DamageGainPercent
        {
            get => _damageGainPercent;
            private set => SetProperty(ref _damageGainPercent, value);
        }

        public string EffectiveAttackPowerChangeText => $"{EffectiveAttack:N2} -> {EffectiveAttack2:N2}";

        public void LoadFromSnapshot(PlayerStatSnapshot? snapshot)
        {
            if (snapshot?.Stats is null)
            {
                Recalculate();
                return;
            }

            _isBulkUpdating = true;
            try
            {
                var stats = snapshot.Stats;
                BaseAttack = stats.AttackBase ?? 0;
                GearAttack = stats.AttackGear ?? 0;
                MinAttack = stats.AttackMin ?? 0;
                MaxAttack = stats.AttackMax ?? 0;
                PveAttack = stats.AttackPve ?? 0;
                BossAttack = stats.AttackBoss ?? 0;

                AttackIncreasePercent = stats.AttackIncreasePercent ?? 0;

                DamageBoostPercent = stats.DamageBoostPercent ?? 0;
                WeaponDamageBoostPercent = stats.WeaponDamageBoostPercent ?? 0;
                PveDamageBoostPercent = stats.PveDamageBoostPercent ?? 0;
                BossDamageBoostPercent = stats.BossDamageBoostPercent ?? 0;
                CriticalDamageBoostPercent = stats.CriticalDamageBoostPercent ?? 0;
                PerfectPercent = stats.PerfectPercent ?? 0;
                SmitePercent = stats.SmitePercent ?? 0;
                FrontDamageBoostPercent = stats.FrontDamageBoostPercent ?? 0;
                BackDamageBoostPercent = stats.BackDamageBoostPercent ?? 0;
                RaceDamageBoostPercent = stats.CongniDamageBoostPercent ?? 0;

                CombatSpeedPercent = stats.CombatSpeedPercent ?? 0;
            }
            finally
            {
                _isBulkUpdating = false;
            }

            Recalculate();
        }

        private void SetMainStat(ref double field, double value)
        {
            double normalized = PercentMath.ClampNonNegative(value);
            if (SetProperty(ref field, normalized) && !_isBulkUpdating)
                Recalculate();
        }

        private void SetEnvironment(ref double field, double value, Action<double> persist)
        {
            double normalized = PercentMath.ClampNonNegative(value);
            if (SetProperty(ref field, normalized))
            {
                persist(normalized);
                Recalculate();
            }
        }

        private void SetOption(ref double field, double value)
        {
            if (SetProperty(ref field, value))
                Recalculate();
        }

        private void Recalculate()
        {
            var result = _calculator.Calculate(CreateStats(), CreateEnvironment(), CreateOptionDelta());
            StatWindowAttack = result.StatWindowAttack;
            EffectiveAttack = result.EffectiveAttack;
            TotalDamage = result.TotalDamage;
            EffectiveAttack2 = result.EffectiveAttack2;
            TotalDamage2 = result.TotalDamage2;
            DamageGainPercent = result.DamageGainPercent;
        }

        private StatEfficiencyStats CreateStats()
        {
            return new StatEfficiencyStats
            {
                BaseAttack = BaseAttack,
                GearAttack = GearAttack,
                MinAttack = MinAttack,
                MaxAttack = MaxAttack,
                PveAttack = PveAttack,
                BossAttack = BossAttack,
                AttackIncreasePercent = AttackIncreasePercent,
                DamageBoostPercent = DamageBoostPercent,
                WeaponDamageBoostPercent = WeaponDamageBoostPercent,
                PveDamageBoostPercent = PveDamageBoostPercent,
                BossDamageBoostPercent = BossDamageBoostPercent,
                CriticalDamageBoostPercent = CriticalDamageBoostPercent,
                PerfectPercent = PerfectPercent,
                SmitePercent = SmitePercent,
                FrontDamageBoostPercent = FrontDamageBoostPercent,
                BackDamageBoostPercent = BackDamageBoostPercent,
                RaceDamageBoostPercent = RaceDamageBoostPercent,
                CombatSpeedPercent = CombatSpeedPercent,
            };
        }

        private StatEfficiencyEnvironment CreateEnvironment()
        {
            return new StatEfficiencyEnvironment
            {
                CritChance = CritChance,
                BackAttackRate = BackAttackRate,
                FrontAttackRate = FrontAttackRate,
                AttackType = SelectedAttackType,
                AttackIncreaseCombatPercent = AttackIncreaseCombatPercent,
                PartyDamageBoost = PartyDamageBoost,
                BossDamageTolerance = BossDamageTolerance,
                PartySmiteBuff = PartySmiteBuff,
                BossSmiteResist = BossSmiteResist,
            };
        }

        private StatEfficiencyOptionDelta CreateOptionDelta()
        {
            return new StatEfficiencyOptionDelta
            {
                BaseAttack = OptionBaseAttack,
                GearAttack = OptionGearAttack,
                MinAttack = OptionMinAttack,
                PveAttack = OptionPveAttack,
                BossAttack = OptionBossAttack,
                AttackIncreasePercent = OptionAttackIncreasePercent,
                DamageBoostPercent = OptionDamageBoostPercent,
                WeaponDamageBoostPercent = OptionWeaponDamageBoostPercent,
                CriticalDamageBoostPercent = OptionCriticalDamageBoostPercent,
                PerfectPercent = OptionPerfectPercent,
                SmitePercent = OptionSmitePercent,
                FrontDamageBoostPercent = OptionFrontDamageBoostPercent,
                BackDamageBoostPercent = OptionBackDamageBoostPercent,
            };
        }

        private static AttackType ParseAttackType(string? attackType)
        {
            return attackType switch
            {
                "Front" => AttackType.Front,
                "Back" => AttackType.Back,
                _ => AttackType.None,
            };
        }
    }
}
