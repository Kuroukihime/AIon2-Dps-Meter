using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.UI.Services.Tray;
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
        private readonly TrayService trayService;

        public readonly Dictionary<long, PlayerRenderState> PlayerStates = new();

        public List<PlayerRenderState> Players = new();

        public string CombatDuration = "00:00";
        public bool PinUserOnTop;
        public bool IsGrouped;
        public bool IsSoloActive;
        public bool IsSoloWaiting;
        public bool IsEditable => windowHelper.IsMeterEdit;
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
        private string _lastDisplaySignature = string.Empty;
        private static readonly TimeSpan EditModeFollowUpRender = TimeSpan.FromMilliseconds(100);
        private CancellationTokenSource _cts = new();

        private readonly Func<Task> onStateChanged;

        public MainDpsViewModel(CombatSessionManager sessionManager, IAppSettingsService settingsService, UpdateCheckerService updateChecker, IWindowManagerService windowManager, WindowHelper windowHelper, TrayService trayService, Func<Task> onStateChanged)
        {
            this.sessionManager = sessionManager;
            this.settingsService = settingsService;
            this.updateChecker = updateChecker;
            this.windowManager = windowManager;
            this.windowHelper = windowHelper;
            this.trayService = trayService;
            this.onStateChanged = onStateChanged;
        }

        public void Initialize()
        {
            sessionManager.PingUpdated += OnPingUpdated;
            settingsService.SettingsChanged += OnSettingsChanged;
            windowHelper.WindowStateUpdated += OnEditModeChanged;

            RowScale = settingsService.PlayerRowScale > 0 ? settingsService.PlayerRowScale : 1.0;

            _ = CheckForUpdatesAsync();

            // 10 Hz keeps the readout live; renders still only happen when a displayed value changes, and bars glide via CSS.
            _refreshTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
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
            // While the move key is held the meter is being positioned: pausing data renders keeps its
            // browser idle so the drag and button clicks are handled immediately instead of queuing behind updates.
            if (windowHelper.IsMoveKeyHeld) return;

            CombatDuration = sessionManager.GetCombatDuration().ToString(@"mm\:ss");

            var targetInfo = sessionManager.GetActiveTargetInfo();
            HasActiveTarget = targetInfo is not null;
            if (targetInfo is not null)
            {
                ActiveTargetName = targetInfo.Name;
                ActiveTargetHpPercentage = targetInfo.HpTotal > 0 ? (double)targetInfo.HpCurrent / targetInfo.HpTotal * 100 : 0;
                ActiveTargetHpDisplay = targetInfo.HpTotal > 0 ? $"{DamageFormatter.Format(targetInfo.HpCurrent)} / {DamageFormatter.Format(targetInfo.HpTotal)}" : string.Empty;
            }

            TotalRaidDamageFormatted = $"{DamageFormatter.Format(sessionManager.GetPartyDps())}/s";

            IsSoloActive = sessionManager.IsSoloActive;

            bool pinUserOnTop = settingsService.PinUserOnTop;
            PinUserOnTop = pinUserOnTop;

            // Solo lists only the user and grouped play only the group; everyone is still recorded.
            bool isGrouped = sessionManager.IsGrouped;
            IsGrouped = isGrouped;
            var allStats = sessionManager.PlayerStats;
            var listedStats = MeterRowFilter.Apply(allStats, isGrouped, IsSoloActive, out bool waitingForUser);
            // The hint only makes sense while other players' damage is listed and none of it is the user's.
            IsSoloWaiting = waitingForUser && allStats.Count > 0;
            bool soloFiltered = IsSoloActive;
            var currentStats = listedStats
                .Where(r => soloFiltered || r.IsIdentified || r.DamagePercentage > 1 || (pinUserOnTop && r.IsUser && r.TotalDamage > 0))
                .ToList();
            bool shareOverShown = isGrouped || soloFiltered;

            long topDamage = currentStats.Count > 0 ? currentStats.Max(x => x.TotalDamage) : 0;
            long shownDamage = currentStats.Sum(x => x.TotalDamage);

            var currentIds = new HashSet<long>();

            bool isNicknameHidden = settingsService.IsNicknameHidden;
            bool showPlayerDeaths = settingsService.ShowPlayerDeaths;
            bool showItemLevel = settingsService.ShowItemLevel;
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
                player.Group = stat.Group;
                player.ClassId = stat.ClassId.ToString();
                player.TotalDamage = stat.TotalDamage;
                player.TotalDamageFormatted = DamageFormatter.Format(stat.TotalDamage);
                player.DpsFormatted = DamageFormatter.Format(stat.DamagePerSecond);
                player.DamagePercentage = shareOverShown
                    ? (shownDamage > 0 ? (double)stat.TotalDamage / shownDamage * 100.0 : 0)
                    : stat.DamagePercentage;
                player.CombatPower = DamageFormatter.Format(stat.CombatPower);
                player.ItemLevel = showItemLevel && stat.ItemLevel > 0 ? stat.ItemLevel.ToString() : string.Empty;
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

                player.EffectivePercentage = useRelativeBar
                    ? (topDamage > 0 ? (double)stat.TotalDamage / topDamage * 100.0 : 0)
                    : player.DamagePercentage;
            }

            foreach (var key in PlayerStates.Keys.Where(k => !currentIds.Contains(k)).ToList())
                PlayerStates.Remove(key);

            var ranked = PlayerStates.Values
                .OrderByDescending(p => p.TotalDamage)
                .ToList();
            for (int i = 0; i < ranked.Count; i++)
                ranked[i].Rank = i + 1;

            Players = pinUserOnTop
                ? ranked.OrderByDescending(p => p.IsUser).ToList()
                : ranked;

            // Re-render only when something on screen changed; idle ticks and identical readouts cost the browser nothing.
            var signature = BuildDisplaySignature();
            if (signature == _lastDisplaySignature) return;
            _lastDisplaySignature = signature;
            onStateChanged.Invoke();
        }

        private string BuildDisplaySignature()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(CombatDuration).Append('|').Append(TotalRaidDamageFormatted).Append('|').Append(PingDisplay)
              .Append('|').Append(HasActiveTarget).Append('|').Append(ActiveTargetName).Append('|').Append(ActiveTargetHpDisplay)
              .Append('|').Append(ActiveTargetHpPercentage.ToString("F1")).Append('|').Append(PinUserOnTop).Append('|').Append(IsGrouped).Append(IsSoloActive).Append(IsSoloWaiting);
            foreach (var p in Players)
            {
                sb.Append('#').Append(p.PlayerId).Append(p.PlayerNameDisplay).Append(p.DpsFormatted).Append(p.TotalDamageFormatted)
                  .Append(p.DamagePercentage.ToString("F1")).Append(p.EffectivePercentage.ToString("F1")).Append(p.CriticalRate.ToString("F1"))
                  .Append(p.DeathsDisplay).Append(p.CombatPower).Append(p.ItemLevel).Append(p.Rank).Append(p.ClassId).Append(p.IsUser).Append(p.Group);
            }
            return sb.ToString();
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

        private void OnEditModeChanged(object? sender, EventArgs e)
        {
            onStateChanged.Invoke();
            _ = RenderAgainAsync();
        }

        // The meter's browser can keep showing the previous frame until another render arrives, and with
        // change-only rendering none may come for a while; a follow-up render makes the edit outline match the key state.
        private async Task RenderAgainAsync()
        {
            try
            {
                await Task.Delay(EditModeFollowUpRender, _cts.Token);
                await onStateChanged.Invoke();
            }
            catch (OperationCanceledException) { }
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
        public string GetSelfClass(PlayerRenderState player) => player.IsUser ? "is-self" : string.Empty;
        public string GetRankPrefix(PlayerRenderState player) => PinUserOnTop && player.IsUser ? $"#{player.Rank} " : string.Empty;

        public string GetGroupIcon(PlayerRenderState player) => player.Group switch
        {
            GroupKind.Party => "👥 ",
            GroupKind.Force => "⚔️ ",
            _ => string.Empty
        };
        public double ClampPercent(double value) => Math.Max(0, Math.Min(100, value));

        public string GetRowScaleStyle() => RowScale != 1.0
            ? $"zoom:{RowScale.ToString(System.Globalization.CultureInfo.InvariantCulture)};"
            : string.Empty;

        public void BeginDrag(MouseEventArgs _) => windowManager.Drag(WindowKey.Main);

        public void OpenHistory() => windowHelper.OpenHistory();
        public void OpenStatEffCalc() => windowHelper.OpenStatEff();
        public void OpenWhatsNew() => windowHelper.OpenWhatsNewWindow();

        public void OpenSettings() => windowHelper.OpenSettings();

        public string SoloClass => IsSoloActive ? "is-on" : "is-off";

        public string SoloTitle => IsGrouped
            ? "Solo is off while you're in a party or force"
            : IsSoloActive ? "Solo: showing only you and your damage (click to show everyone)" : "Click to show only you and your damage";

        // Updates the readout at once: data renders are paused while the move key is held to click.
        public void ToggleSolo()
        {
            if (IsGrouped) return;
            settingsService.TotalShowsOnlyMyDps = !settingsService.TotalShowsOnlyMyDps;
            IsSoloActive = sessionManager.IsSoloActive;
            TotalRaidDamageFormatted = $"{DamageFormatter.Format(sessionManager.GetPartyDps())}/s";
            onStateChanged.Invoke();
        }
        public void Minimize() => trayService.HideToTray();
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
            windowHelper.WindowStateUpdated -= OnEditModeChanged;
        }

    }
}
