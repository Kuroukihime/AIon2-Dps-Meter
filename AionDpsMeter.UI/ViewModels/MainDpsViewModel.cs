using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.UI.Services.Windowing;
using AionDpsMeter.UI.Utils;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Concurrent;

namespace AionDpsMeter.UI.ViewModels
{
    public class MainDpsViewModel : IDisposable
    {
        private readonly CombatSessionManager sessionManager;
        private readonly IAppSettingsService settingsService;
        private readonly UpdateCheckerService updateChecker;
        private readonly IWindowManagerService windowManager;
        private readonly WindowHelper windowHelper;

        public readonly Dictionary<long, PlayerRenderState> PlayerStates = new();

        public List<PlayerRenderState> Players = new();

        public string CombatDuration = "00:00";
        public string TotalRaidDamageFormatted = "0/s";
        public string PingDisplay = "-- ms";
        public string PingColor = "#888888";
        public int PingLevel = 0;

        public bool HasActiveTarget;
        public string ActiveTargetName = string.Empty;
        public string ActiveTargetHpDisplay = string.Empty;
        public double ActiveTargetHpPercentage;

        public bool UpdateAvailable;
        public string UpdateVersionText = string.Empty;

        public double RowScale = 1.0;

        private readonly ConcurrentDictionary<string, string> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        private PeriodicTimer? _refreshTimer;
        private CancellationTokenSource _cts = new();

        private readonly Func<Task> onStateChanged;

        public MainDpsViewModel(CombatSessionManager sessionManager, IAppSettingsService settingsService, UpdateCheckerService updateChecker, IWindowManagerService windowManager, WindowHelper windowHelper, Func<Task> onStateChanged)
        {
            this.sessionManager = sessionManager;
            this.settingsService = settingsService;
            this.updateChecker = updateChecker;
            this.windowManager = windowManager;
            this.windowHelper = windowHelper;
            this.onStateChanged = onStateChanged;
        }

        public void Initialize()
        {
            sessionManager.PingUpdated += OnPingUpdated;
            settingsService.SettingsChanged += OnSettingsChanged;

            RowScale = settingsService.PlayerRowScale > 0 ? settingsService.PlayerRowScale : 1.0;

            _ = CheckForUpdatesAsync();

            _refreshTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
            _ = UpdateLoopAsync();
        }
        private async Task UpdateLoopAsync()
        {
            try
            {
                while (await _refreshTimer!.WaitForNextTickAsync(_cts.Token))
                {
                    UpdateData();
                }
            }
            catch (OperationCanceledException) { }
        }

        private void UpdateData()
        {
            bool uiNeedsUpdate = false;

            var newDuration = sessionManager.GetCombatDuration().ToString(@"mm\:ss");
            if (CombatDuration != newDuration)
            {
                CombatDuration = newDuration;
                uiNeedsUpdate = true;
            }

            var targetInfo = sessionManager.GetActiveTargetInfo();
            if (targetInfo is not null)
            {
                HasActiveTarget = true;
                ActiveTargetName = targetInfo.Name;
                ActiveTargetHpPercentage = targetInfo.HpTotal > 0 ? (double)targetInfo.HpCurrent / targetInfo.HpTotal * 100 : 0;
                ActiveTargetHpDisplay = targetInfo.HpTotal > 0 ? $"{DamageFormatter.Format(targetInfo.HpCurrent)} / {DamageFormatter.Format(targetInfo.HpTotal)}" : string.Empty;
                uiNeedsUpdate = true;
            }
            else if (HasActiveTarget)
            {
                HasActiveTarget = false;
                uiNeedsUpdate = true;
            }

            TotalRaidDamageFormatted = $"{DamageFormatter.Format(sessionManager.GetPartyDps())}/s";

            var currentStats = sessionManager.PlayerStats
                .Where(r => r.IsIdentified || r.DamagePercentage > 1)
                .ToList();

            long topDamage = currentStats.Count > 0 ? currentStats.Max(x => x.TotalDamage) : 0;
            var currentIds = new HashSet<long>();

            bool isNicknameHidden = settingsService.IsNicknameHidden;
            bool showPlayerDeaths = settingsService.ShowPlayerDeaths;
            bool useRelativeBar = settingsService.RelativeProgressBar;

            foreach (var stat in currentStats)
            {
                currentIds.Add(stat.PlayerId);

                if (!PlayerStates.TryGetValue(stat.PlayerId, out var player))
                {
                    player = new PlayerRenderState { PlayerId = stat.PlayerId };
                    PlayerStates[stat.PlayerId] = player;
                }

                player.IsUser = stat.IsUser;
                player.ClassId = stat.ClassId.ToString();
                player.TotalDamage = stat.TotalDamage;
                player.TotalDamageFormatted = DamageFormatter.Format(stat.TotalDamage);
                player.DpsFormatted = DamageFormatter.Format(stat.DamagePerSecond);
                player.DamagePercentage = stat.DamagePercentage;
                player.CombatPower = DamageFormatter.Format(stat.CombatPower);
                player.IconUrl = ResolveClassIconUrl(stat.ClassId);
                player.ClassName = stat.ClassName;
                player.ServerName = stat.ServerName;
                player.ClassIcon = stat.ClassIcon;
                player.CriticalRate = stat.CriticalRate;

                string rawName = stat.PlayerName;

                player.PlayerNameDisplay = isNicknameHidden
                    ? NicknameObfuscator.Mask(rawName)
                    : rawName;

                player.DeathsDisplay = (showPlayerDeaths && stat.PlayerDeaths > 0)
                    ? $"💀 {stat.PlayerDeaths}"
                    : string.Empty;

                double targetAbs = stat.DamagePercentage;
                double diffAbs = targetAbs - player.VisualAbsolutePercentage;
                if (Math.Abs(diffAbs) < 0.05) player.VisualAbsolutePercentage = targetAbs;
                else player.VisualAbsolutePercentage += diffAbs * 0.25;

                double targetRel = topDamage > 0 ? ((double)stat.TotalDamage / topDamage) * 100.0 : 0;
                double diffRel = targetRel - player.VisualRelativePercentage;
                if (Math.Abs(diffRel) < 0.05) player.VisualRelativePercentage = targetRel;
                else player.VisualRelativePercentage += diffRel * 0.25;

                player.EffectivePercentage = useRelativeBar
                    ? player.VisualRelativePercentage
                    : player.VisualAbsolutePercentage;

                uiNeedsUpdate = true;
            }

            var keysToRemove = PlayerStates.Keys.Where(k => !currentIds.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                PlayerStates.Remove(key);
                uiNeedsUpdate = true;
            }

            if (uiNeedsUpdate)
            {
                Players = PlayerStates.Values
                    .OrderByDescending(p => p.TotalDamage)
                    .ToList();

                onStateChanged.Invoke();
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            var release = await updateChecker.CheckForUpdateAsync();
            if (release is not null)
            {
                UpdateVersionText = $"New version available: {release.Name}";
                UpdateAvailable = true;
                await onStateChanged.Invoke();
            }
        }

        private void OnPingUpdated(object? sender, int pingMs)
        {
            PingDisplay = $"{pingMs} ms";
            if (pingMs < 60) { PingColor = "#4EC9B0"; PingLevel = 3; }
            else if (pingMs < 100) { PingColor = "#DCDCAA"; PingLevel = 2; }
            else if (pingMs < 200) { PingColor = "#CE9178"; PingLevel = 1; }
            else { PingColor = "#F44747"; PingLevel = 1; }
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            RowScale = settingsService.PlayerRowScale > 0 ? settingsService.PlayerRowScale : 1.0;
            onStateChanged.Invoke();
        }

        private string ResolveClassIconUrl(int classId)
        {
            string key = classId.ToString();
            if (_iconCache.TryGetValue(key, out var cached)) return cached;

            string resolved = $"/images/classes/{classId}.png";
            _iconCache[key] = resolved;
            return resolved;
        }

        public string GetPlayerRowClass(PlayerRenderState player)
        {
            if (!settingsService.UseClassColors)
            {
                return player.IsUser ? "is-me" : "player-color ";
            }
            else
            {
                return $"dps-class-{player.ClassId}";
            }
        }

        public string GetIsUserClass(PlayerRenderState player)
        {
            if (!settingsService.UseClassColors) return string.Empty;
            return player.IsUser ? "is-me-border" : string.Empty;
        }

        //public string GetProgressClass(PlayerRenderState player) => $"dps-class-{player.ClassId}";
        public string GetCombatScoreDisplay(PlayerRenderState player) => (string.IsNullOrWhiteSpace(player.CombatPower) || player.CombatPower == "0") ? "" : player.CombatPower;
        public double ClampPercent(double value) => Math.Max(0, Math.Min(100, value));

        public string GetRowScaleStyle() => RowScale != 1.0
            ? $"zoom:{RowScale.ToString(System.Globalization.CultureInfo.InvariantCulture)};"
            : string.Empty;

        public void BeginDrag(MouseEventArgs _) => windowManager.Drag(WindowKey.Main);

        public void OpenHistory() => windowHelper.OpenHistory();
        public void OpenStatEffCalc() => windowHelper.OpenStatEff();
        public void OpenWhatsNew() => windowHelper.OpenWhatsNewWindow();

        public void OpenSettings() => windowHelper.OpenSettings();
        public void Minimize() => windowManager.Minimize(WindowKey.Main);
        public void Close() => windowManager.CloseApplication();
        public void DismissUpdate() { UpdateAvailable = false; onStateChanged.Invoke(); }
        public void OpenPlayerDetails(PlayerRenderState player) => windowHelper.OpenPlayerDetails(player);

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _refreshTimer?.Dispose();
            sessionManager.PingUpdated -= OnPingUpdated;
            settingsService.SettingsChanged -= OnSettingsChanged;
        }

    }
}
