using AionDpsMeter.UI.Services.UiCommands;
using AionDpsMeter.UI.ViewModels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace AionDpsMeter.UI.Pages
{
    public partial class MainDpsMinimal : ComponentBase, IDisposable
    {
        [Inject] private IUiCommandService UiCommandService { get; set; } = default!;

        [Inject] private IServiceProvider Services { get; set; } = default!;

        private MainViewModel? _mainViewModel;
        private readonly ConcurrentDictionary<string, string?> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        private MainViewModel? Vm => _mainViewModel;

        protected override void OnInitialized()
        {
            _mainViewModel = Services.GetService<MainViewModel>();
            if (_mainViewModel is null)
                return;

            _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
            _mainViewModel.Players.CollectionChanged += OnPlayersCollectionChanged;
            SubscribeToPlayerChanges(_mainViewModel.Players);
        }

        private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
            => RequestRender();

        private void OnPlayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (var item in e.OldItems.OfType<PlayerStatsViewModel>())
                    item.PropertyChanged -= OnPlayerPropertyChanged;
            }

            if (e.NewItems is not null)
            {
                foreach (var item in e.NewItems.OfType<PlayerStatsViewModel>())
                    item.PropertyChanged += OnPlayerPropertyChanged;
            }

            RequestRender();
        }

        private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
            => RequestRender();

        private void SubscribeToPlayerChanges(IEnumerable<PlayerStatsViewModel> players)
        {
            foreach (var player in players)
                player.PropertyChanged += OnPlayerPropertyChanged;
        }

        private void UnsubscribeFromPlayerChanges(IEnumerable<PlayerStatsViewModel> players)
        {
            foreach (var player in players)
                player.PropertyChanged -= OnPlayerPropertyChanged;
        }

        private void RequestRender()
            => _ = InvokeAsync(StateHasChanged);

        private static double ClampPercent(double value)
            => Math.Max(0, Math.Min(100, value));

        private static string GetProgressClass(PlayerStatsViewModel player)
            => $"dps-class-{player.ClassId}";

        private static string GetPingColor(string? pingColor)
            => string.IsNullOrWhiteSpace(pingColor) ? "#888" : pingColor;

        private static int GetPingLevel(string? pingDisplay)
        {
            if (string.IsNullOrWhiteSpace(pingDisplay))
                return 0;

            var digits = new string(pingDisplay.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, out var pingMs))
                return 0;

            return pingMs switch
            {
                < 60 => 3,
                < 100 => 2,
                < 200 => 1,
                _ => 1
            };
        }

        private static string GetCombatScoreDisplay(PlayerStatsViewModel player)
            => (string.IsNullOrWhiteSpace(player.CombatPower) || player.CombatPower == "0") ? "" : player.CombatPower;

        private static string GetNameTag(PlayerStatsViewModel player)
            => string.IsNullOrWhiteSpace(player.ServerName) ? string.Empty : $"[{player.ServerName}]";

        private static string GetScore(PlayerStatsViewModel player)
            => player.CombatPower;

        private static string FormatDpsMain(PlayerStatsViewModel player)
            => player.DpsFormatted;

        private static string FormatDpsSuffix(PlayerStatsViewModel player)
            => "/s";

        private string ResolveClassIconUrl(PlayerStatsViewModel player)
        {
            var iconPath = player.ClassIcon;
            if (string.IsNullOrWhiteSpace(iconPath))
                return string.Empty;

            if (_iconCache.TryGetValue(iconPath, out var cached))
                return cached ?? string.Empty;

            string? resolved;
            if (iconPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                iconPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                iconPath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                resolved = iconPath;
            }
            else
            {
                resolved = TryCreateEmbeddedDataUri(iconPath);
            }

            _iconCache[iconPath] = resolved;
            return resolved ?? string.Empty;
        }

        private static string? TryCreateEmbeddedDataUri(string iconPath)
        {
            try
            {
                var normalized = iconPath.StartsWith('/') ? iconPath : $"/{iconPath}";
                var uri = new Uri($"pack://application:,,,{normalized}", UriKind.Absolute);
                var streamInfo = Application.GetResourceStream(uri);
                if (streamInfo?.Stream is null)
                    return null;

                using var ms = new MemoryStream();
                using (streamInfo.Stream)
                {
                    streamInfo.Stream.CopyTo(ms);
                }

                var bytes = ms.ToArray();
                var contentType = GetContentType(iconPath);
                return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
            }
            catch
            {
                return null;
            }
        }

        private static string GetContentType(string path)
        {
            if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                return "image/jpeg";
            if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                return "image/gif";
            if (path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                return "image/webp";
            return "image/png";
        }

        private void BeginDrag(MouseEventArgs _)
            => UiCommandService.Request(new UiCommandRequest(UiCommandType.BeginMainWindowDrag));

        private void OpenHistory()
            => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenHistory));
        private void OpenStatEffCalc()
            => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenStatEff));

        private void OpenWhatsNew()
            => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenWhatsNew));

        private void DismissUpdate()
            => Vm?.DismissUpdateCommand?.Execute(null);

        private void OpenSettings()
        {
            UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenSettings));
        }

        private void Minimize() => UiCommandService.Request(new UiCommandRequest(UiCommandType.MinimizeMainWindow));

        private void Close() => UiCommandService.Request(new UiCommandRequest(UiCommandType.CloseApplication));

        private void OpenPlayerDetails(PlayerStatsViewModel player)
            => UiCommandService.Request(new UiCommandRequest(UiCommandType.OpenPlayerDetails, player.PlayerId, player.PlayerName));

        public void Dispose()
        {
            if (_mainViewModel is null)
                return;

            _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
            _mainViewModel.Players.CollectionChanged -= OnPlayersCollectionChanged;
            UnsubscribeFromPlayerChanges(_mainViewModel.Players);
        }
    }
}