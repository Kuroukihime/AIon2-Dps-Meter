using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.UI.Services.UiCommands;
using AionDpsMeter.UI.Utils; 
using AionDpsMeter.UI.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Concurrent;
using System.IO;
using System.Windows;

namespace AionDpsMeter.UI.Pages
{
    public partial class MainDpsMinimal : ComponentBase, IDisposable
    {
        [Inject] private IUiCommandService UiCommandService { get; set; } = default!;
        [Inject] private CombatSessionManager SessionManager { get; set; } = default!;
        [Inject] private IAppSettingsService SettingsService { get; set; } = default!;
        [Inject] private UpdateCheckerService UpdateChecker { get; set; } = default!;

        private readonly Dictionary<long, PlayerRenderState> _playerStates = new();

        private List<PlayerRenderState> _players = new();

        private string _combatDuration = "00:00";
        private string _totalRaidDamageFormatted = "0/s";
        private string _pingDisplay = "-- ms";
        private string _pingColor = "#888888";
        private int _pingLevel = 0;

        private bool _hasActiveTarget;
        private string _activeTargetName = string.Empty;
        private string _activeTargetHpDisplay = string.Empty;
        private double _activeTargetHpPercentage;

        private bool _updateAvailable;
        private string _updateVersionText = string.Empty;

        private readonly ConcurrentDictionary<string, string> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        private PeriodicTimer? _refreshTimer;
        private CancellationTokenSource _cts = new();

        protected override void OnInitialized()
        {
            SessionManager.PingUpdated += OnPingUpdated;
            SettingsService.SettingsChanged += OnSettingsChanged;

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

            var newDuration = SessionManager.GetCombatDuration().ToString(@"mm\:ss");
            if (_combatDuration != newDuration)
            {
                _combatDuration = newDuration;
                uiNeedsUpdate = true;
            }

            var targetInfo = SessionManager.GetActiveTargetInfo();
            if (targetInfo is not null)
            {
                _hasActiveTarget = true;
                _activeTargetName = targetInfo.Name;
                _activeTargetHpPercentage = targetInfo.HpTotal > 0 ? (double)targetInfo.HpCurrent / targetInfo.HpTotal * 100 : 0;
                _activeTargetHpDisplay = targetInfo.HpTotal > 0 ? $"{DamageFormatter.Format(targetInfo.HpCurrent)} / {DamageFormatter.Format(targetInfo.HpTotal)}" : string.Empty;
                uiNeedsUpdate = true;
            }
            else if (_hasActiveTarget)
            {
                _hasActiveTarget = false;
                uiNeedsUpdate = true;
            }

            _totalRaidDamageFormatted = $"{DamageFormatter.Format(SessionManager.GetPartyDps())}/s";

            var currentStats = SessionManager.PlayerStats
                .Where(r => r.IsIdentified || r.DamagePercentage > 1)
                .ToList();

            long topDamage = currentStats.Count > 0 ? currentStats.Max(x => x.TotalDamage) : 0;
            var currentIds = new HashSet<long>();

            bool isNicknameHidden = SettingsService.IsNicknameHidden;
            bool showPlayerDeaths = SettingsService.ShowPlayerDeaths;
            bool useRelativeBar = SettingsService.RelativeProgressBar;

            foreach (var stat in currentStats)
            {
                currentIds.Add(stat.PlayerId);

                if (!_playerStates.TryGetValue(stat.PlayerId, out var player))
                {
                    player = new PlayerRenderState { PlayerId = stat.PlayerId };
                    _playerStates[stat.PlayerId] = player;
                }

                player.IsUser = stat.IsUser;
                player.ClassId = stat.ClassId.ToString();
                player.TotalDamage = stat.TotalDamage;
                player.TotalDamageFormatted = DamageFormatter.Format(stat.TotalDamage);
                player.DpsFormatted = DamageFormatter.Format(stat.DamagePerSecond);
                player.DamagePercentage = stat.DamagePercentage;
                player.CombatPower = DamageFormatter.Format(stat.CombatPower);
                player.IconUrl = ResolveClassIconUrl(stat.ClassIcon);

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

            var keysToRemove = _playerStates.Keys.Where(k => !currentIds.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                _playerStates.Remove(key);
                uiNeedsUpdate = true;
            }

            if (uiNeedsUpdate)
            {
                _players = _playerStates.Values
                    .OrderByDescending(p => p.TotalDamage)
                    .ToList();

                InvokeAsync(StateHasChanged);
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            var release = await UpdateChecker.CheckForUpdateAsync();
            if (release is not null)
            {
                _updateVersionText = $"New version available: {release.Name}";
                _updateAvailable = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        private void OnPingUpdated(object? sender, int pingMs)
        {
            _pingDisplay = $"{pingMs} ms";
            if (pingMs < 60) { _pingColor = "#4EC9B0"; _pingLevel = 3; }
            else if (pingMs < 100) { _pingColor = "#DCDCAA"; _pingLevel = 2; }
            else if (pingMs < 200) { _pingColor = "#CE9178"; _pingLevel = 1; }
            else { _pingColor = "#F44747"; _pingLevel = 1; }
        }


        private void OnSettingsChanged(object? sender, EventArgs e)
            => InvokeAsync(StateHasChanged);

        private string ResolveClassIconUrl(string? iconPath)
        {
            if (string.IsNullOrWhiteSpace(iconPath)) return string.Empty;
            if (_iconCache.TryGetValue(iconPath, out var cached)) return cached;
            string resolved = (iconPath.StartsWith("http") || iconPath.StartsWith("data:")) ? iconPath : TryCreateEmbeddedDataUri(iconPath) ?? string.Empty;
            _iconCache[iconPath] = resolved;
            return resolved;
        }

        private static string? TryCreateEmbeddedDataUri(string iconPath)
        {
            try
            {
                var normalized = iconPath.StartsWith('/') ? iconPath : $"/{iconPath}";
                var uri = new Uri($"pack://application:,,,{normalized}", UriKind.Absolute);
                var streamInfo = Application.GetResourceStream(uri);
                if (streamInfo?.Stream is null) return null;
                using var ms = new MemoryStream();
                streamInfo.Stream.CopyTo(ms);
                string contentType = iconPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || iconPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg" :
                                     iconPath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? "image/gif" :
                                     iconPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp" : "image/png";
                return $"data:{contentType};base64,{Convert.ToBase64String(ms.ToArray())}";
            }
            catch { return null; }
        }

        private string GetProgressClass(PlayerRenderState player) => $"dps-class-{player.ClassId}";
        private string GetCombatScoreDisplay(PlayerRenderState player) => (string.IsNullOrWhiteSpace(player.CombatPower) || player.CombatPower == "0") ? "" : player.CombatPower;
        private double ClampPercent(double value) => Math.Max(0, Math.Min(100, value));

        private void BeginDrag(MouseEventArgs _) => UiCommandService.Request(new UiCommandRequest(UiCommandType.BeginMainWindowDrag));
        private void OpenHistory() => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenHistory));
        private void OpenStatEffCalc() => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenStatEff));
        private void OpenWhatsNew() => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenWhatsNew));
        private void OpenSettings() => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenSettings));
        private void Minimize() => UiCommandService.Request(new UiCommandRequest(UiCommandType.MinimizeMainWindow));
        private void Close() => UiCommandService.Request(new UiCommandRequest(UiCommandType.CloseApplication));
        private void DismissUpdate() { _updateAvailable = false; StateHasChanged(); }
        private void OpenPlayerDetails(PlayerRenderState player) => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenPlayerDetails, player.PlayerId, player.PlayerNameDisplay));

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _refreshTimer?.Dispose();
            SessionManager.PingUpdated -= OnPingUpdated;
            SettingsService.SettingsChanged -= OnSettingsChanged;
        }

        public class PlayerRenderState
        {
            public long PlayerId { get; set; }
            public bool IsUser { get; set; }
            public string ClassId { get; set; } = string.Empty;

            public string PlayerNameDisplay { get; set; } = string.Empty;
            public string DeathsDisplay { get; set; } = string.Empty;

            public long TotalDamage { get; set; }
            public string TotalDamageFormatted { get; set; } = string.Empty;
            public string DpsFormatted { get; set; } = string.Empty;
            public double DamagePercentage { get; set; }
            public string CombatPower { get; set; } = string.Empty;
            public string IconUrl { get; set; } = string.Empty;

            public double VisualAbsolutePercentage { get; set; }
            public double VisualRelativePercentage { get; set; }
            public double EffectivePercentage { get; set; } 
        }
    }
}