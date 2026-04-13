using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Utils;
using AionDpsMeter.UI.ViewModels.History;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class PlayerDetailsViewModel : ViewModelBase, IDisposable
    {
        private readonly CombatSessionManager? _sessionManager;
        private readonly IAppSettingsService _settingsService;
        private readonly long _playerId;
        private readonly string? _playerIcon;
        private readonly string? _classIcon;
        private readonly DispatcherTimer? _updateTimer;
        private int _knownCombatLogCount;

        /// <summary>True when this VM was created from a history snapshot
        public bool IsSnapshot { get; }

        [ObservableProperty] private ObservableCollection<SkillStatsViewModel> _skills = new();
        [ObservableProperty] private ObservableCollection<CombatLogEntryViewModel> _combatLog = new();
        [ObservableProperty] private ObservableCollection<BuffStatsViewModel> _buffs = new();
        [ObservableProperty] private int _selectedTabIndex;
        [ObservableProperty] private string _playerNameDisplay;
        [ObservableProperty] private string _serverName = string.Empty;
        [ObservableProperty] private string _classNameDisplay = string.Empty;
        [ObservableProperty] private string? _playerIconDisplay;
        [ObservableProperty] private string? _classIconDisplay;
        [ObservableProperty] private int _combatPower;

        // ?? Summary stats ??????????????????????????????????????????????????????
        [ObservableProperty] private string _totalDamageDisplay = "0";
        [ObservableProperty] private string _dpsDisplay = "0";
        [ObservableProperty] private int _totalHits;
        [ObservableProperty] private string _criticalRateDisplay = "0%";
        [ObservableProperty] private string _backAttackRateDisplay = "0%";
        [ObservableProperty] private string _perfectRateDisplay = "0%";
        [ObservableProperty] private string _doubleDamageRateDisplay = "0%";
        [ObservableProperty] private string _parryRateDisplay = "0%";
        [ObservableProperty] private string _damageContributionDisplay = "0%";
        [ObservableProperty] private string _combatDurationDisplay = "00:00";
        [ObservableProperty] private int _skillCount;
        [ObservableProperty] private int _buffCount;

        // ?? Active target ??????????????????????????????????????????????????????
        [ObservableProperty] private string _activeTargetName = string.Empty;
        [ObservableProperty] private int _activeTargetHpTotal;
        [ObservableProperty] private string _activeTargetHpTotalDisplay = string.Empty;
        [ObservableProperty] private bool _hasActiveTarget;

        // ?? View toggle ????????????????????????????????????????????????????????
        [ObservableProperty] private bool _showCombatLog;
        public bool ShowSkills => !ShowCombatLog;
        partial void OnShowCombatLogChanged(bool value) => OnPropertyChanged(nameof(ShowSkills));

        /// <summary>Nickname formatted as <c>Name[Server]</c> when server is known, otherwise just <c>Name</c>.</summary>
        public string PlayerNameWithServer
        {
            get
            {
                string name = _settingsService.IsNicknameHidden
                    ? NicknameObfuscator.Mask(_playerNameDisplay)
                    : _playerNameDisplay;
                return string.IsNullOrEmpty(_serverName)
                    ? name
                    : $"{name}[{_serverName}]";
            }
        }

        public bool HasPlayerIcon => !string.IsNullOrEmpty(_playerIcon);
        public bool HasClassIcon  => !string.IsNullOrEmpty(_classIcon);

        /// <summary>
        /// Creates a snapshot (read-only, no timer) VM from a <see cref="HistoryPlayerViewModel"/>.
        /// Used when opening player details from the history window.
        /// </summary>
        public static PlayerDetailsViewModel FromSnapshot(
            HistoryPlayerViewModel player,
            IAppSettingsService settingsService,
            string targetName)
        {
            var vm = new PlayerDetailsViewModel(
                sessionManager: null,
                playerId: player.PlayerId,
                playerName: player.PlayerName,
                className: player.ClassName,
                playerIcon: player.PlayerIcon,
                classIcon: player.ClassIcon,
                settingsService: settingsService,
                combatPower: player.CombatPower,
                serverName: player.ServerName,
                isSnapshot: true);
            vm.TotalDamageDisplay        = player.TotalDamageDisplay;
            vm.DpsDisplay                = player.DpsDisplay;
            vm.TotalHits                 = player.HitCount;
            vm.CriticalRateDisplay       = player.CritRateDisplay;
            vm.BackAttackRateDisplay     = player.BackAttackRateDisplay;
            vm.PerfectRateDisplay        = player.PerfectRateDisplay;
            vm.DoubleDamageRateDisplay   = player.DoubleDamageRateDisplay;
            vm.ParryRateDisplay          = player.ParryRateDisplay;
            vm.DamageContributionDisplay = player.DamagePercentDisplay;
            vm.CombatDurationDisplay     = player.DurationDisplay;

            vm.HasActiveTarget            = !string.IsNullOrEmpty(targetName);
            vm.ActiveTargetName           = targetName;

            foreach (var skill in player.Skills)
                vm.Skills.Add(skill);
            vm.SkillCount = vm.Skills.Count;

            foreach (var buff in player.Buffs)
                vm.Buffs.Add(buff);
            vm.BuffCount = vm.Buffs.Count;

            return vm;
        }

        public PlayerDetailsViewModel(
            CombatSessionManager? sessionManager,
            long playerId,
            string playerName,
            string className,
            string? playerIcon,
            string? classIcon,
            IAppSettingsService settingsService,
            int combatPower = 0,
            string serverName = "",
            bool isSnapshot = false)
        {
            IsSnapshot         = isSnapshot;
            _sessionManager    = sessionManager;
            _settingsService   = settingsService;
            _playerId          = playerId;
            _playerIcon        = playerIcon;
            _classIcon         = classIcon;
            _playerNameDisplay = playerName;
            _classNameDisplay  = className;
            _playerIconDisplay = playerIcon;
            _classIconDisplay  = classIcon;
            _combatPower       = combatPower;
            _serverName        = serverName;

            _settingsService.SettingsChanged += OnSettingsChanged;

            if (!isSnapshot)
            {
                _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(66) };
                _updateTimer.Tick += OnUpdateTimerTick;
                _updateTimer.Start();
                RefreshData();
            }
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(PlayerNameWithServer));
        }

        [RelayCommand]
        private void ToggleCombatLog() => ShowCombatLog = !ShowCombatLog;

        private void OnUpdateTimerTick(object? sender, EventArgs e) => RefreshData();

        private void RefreshData()
        {
            RefreshSkills();
            RefreshBuffs();
            RefreshCombatLog();
            RefreshPlayerSummary();
        }

        private void RefreshPlayerSummary()
        {
            if (_sessionManager is null) return;

            var playerStats = _sessionManager.PlayerStats.FirstOrDefault(p => p.PlayerId == _playerId);
            if (playerStats is not null)
            {
                TotalDamageDisplay        = DamageFormatter.FormatFull(playerStats.TotalDamage);
                DpsDisplay                = DamageFormatter.Format(playerStats.DamagePerSecond);
                TotalHits                 = playerStats.HitCount;
                CriticalRateDisplay       = DamageFormatter.FormatRate(playerStats.CriticalRate);
                BackAttackRateDisplay     = DamageFormatter.FormatRate(playerStats.BackAttackRate);
                PerfectRateDisplay        = DamageFormatter.FormatRate(playerStats.PerfectRate);
                DoubleDamageRateDisplay   = DamageFormatter.FormatRate(playerStats.DoubleDamageRate);
                ParryRateDisplay          = DamageFormatter.FormatRate(playerStats.ParryRate);
                DamageContributionDisplay = DamageFormatter.FormatRate(playerStats.DamagePercentage);
                CombatDurationDisplay     = DamageFormatter.FormatDuration(playerStats.CombatDuration);
                if (playerStats.CombatPower > 0)
                    CombatPower = playerStats.CombatPower;
                if (!string.IsNullOrEmpty(playerStats.ServerName))
                {
                    ServerName = playerStats.ServerName;
                    OnPropertyChanged(nameof(PlayerNameWithServer));
                }
            }

            var targetInfo = _sessionManager.GetActiveTargetInfo();
            if (targetInfo is not null)
            {
                HasActiveTarget            = true;
                ActiveTargetName           = targetInfo.Name;
                ActiveTargetHpTotal        = targetInfo.HpTotal;
                ActiveTargetHpTotalDisplay = targetInfo.HpTotal > 0
                    ? $"HP: {DamageFormatter.FormatFull(targetInfo.HpTotal)}"
                    : string.Empty;
            }
            else
            {
                HasActiveTarget            = false;
                ActiveTargetName           = string.Empty;
                ActiveTargetHpTotal        = 0;
                ActiveTargetHpTotalDisplay = string.Empty;
            }
        }

        private void RefreshSkills()
        {
            if (_sessionManager is null) return;

            var skillStats = _sessionManager.GetPlayerSkillStats(_playerId)
                .OrderByDescending(s => s.TotalDamage)
                .ToList();

            foreach (var stats in skillStats)
            {
                var existing = Skills.FirstOrDefault(vm => vm.SkillId == stats.SkillId);
                if (existing is not null)
                    existing.Update(stats);
                else
                    Skills.Add(new SkillStatsViewModel(stats));
            }

            for (int i = Skills.Count - 1; i >= 0; i--)
            {
                if (!skillStats.Any(s => s.SkillId == Skills[i].SkillId))
                    Skills.RemoveAt(i);
            }

            for (int i = 0; i < skillStats.Count; i++)
            {
                int current = Skills.IndexOf(Skills.First(vm => vm.SkillId == skillStats[i].SkillId));
                if (current != i)
                    Skills.Move(current, i);
            }

            SkillCount = Skills.Count;
        }

        private void RefreshBuffs()
        {
            if (_sessionManager is null) return;

            var buffStats = _sessionManager.GetPlayerBuffStats(_playerId);

            if (Buffs.Count == buffStats.Count && BuffsMatch(buffStats))
            {
                return;
            }

            Buffs.Clear();
            foreach (var buff in buffStats)
                Buffs.Add(new BuffStatsViewModel(buff));
            BuffCount = Buffs.Count;
        }

        private bool BuffsMatch(IReadOnlyCollection<Services.Models.BuffStats> newStats)
        {
            int i = 0;
            foreach (var stat in newStats)
            {
                if (i >= Buffs.Count) return false;
                var existing = Buffs[i];
                if (existing.BuffId != stat.BuffId || existing.ApplicationCount != stat.ApplicationCount)
                    return false;
                i++;
            }
            return true;
        }

        private void RefreshCombatLog()
        {
            if (_sessionManager is null) return;

            var allEntries = _sessionManager.GetPlayerCombatLog(_playerId);

            if (allEntries.Count < _knownCombatLogCount)
            {
                CombatLog.Clear();
                _knownCombatLogCount = 0;
                Skills.Clear();
                SkillCount = 0;
            }

        
            if (allEntries.Count > _knownCombatLogCount)
            {
                int newCount = allEntries.Count - _knownCombatLogCount;
              
                for (int i = newCount - 1; i >= 0; i--)
                    CombatLog.Insert(0, new CombatLogEntryViewModel(allEntries[i]));

                _knownCombatLogCount = allEntries.Count;

                while (CombatLog.Count > 200)
                    CombatLog.RemoveAt(CombatLog.Count - 1);
            }
        }

        public void Dispose()
        {
            _settingsService.SettingsChanged -= OnSettingsChanged;
            _updateTimer?.Stop();
        }
    }
}
