using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Timed;
using AionDpsMeter.UI.Services.Windowing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;


namespace AionDpsMeter.UI.Pages
{
    partial class BuffOverlay(IWindowManagerService windowManager, WindowHelper windowHelper, IAppSettingsService appSettingsService, [FromKeyedServices("Buffs")] ITimedEventTracker buffTracker)
    {

        private bool IsEditable => windowHelper.IsBuffEdit;

        private OverlaySettings settings = new();

        protected override void OnInitialized()
        {
            settings = appSettingsService.BufOverlaySettings;

            buffTracker.StateChanged += OnLiveStateChanged;
            appSettingsService.SettingsChanged += OnSettingsChanged;
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
               // _ = TestBuffs();
            }

            return base.OnAfterRenderAsync(firstRender);
        }

        private async Task TestBuffs()
        {

            await Task.Yield();

            var skill = GameDataProvider.Instance.GetSkillOrDefault(13310000);
            var skill2 = GameDataProvider.Instance.GetSkillOrDefault(13390000);

            for (int i = 0; i < 50; i++)
            {

                buffTracker.Register(new TimedEvent()
                {
                    Name = skill.Name,
                    IconUrl = skill.Icon ?? "",
                    Id = (uint)skill.Id,
                    Duration = TimeSpan.FromSeconds(10)

                });
                await Task.Delay(2000);
                buffTracker.Register(new TimedEvent()
                {
                    Name = skill2.Name,
                    IconUrl = skill2.Icon ?? "",
                    Id = (uint)skill2.Id,
                    Duration = TimeSpan.FromSeconds(5)

                });

                await Task.Delay(7000);
            }


        }

        private void OnLiveStateChanged(object? sender, EventArgs e) =>
            InvokeAsync(StateHasChanged);

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            settings = appSettingsService.BufOverlaySettings;
            InvokeAsync(StateHasChanged);
        }

        private void BeginDrag(MouseEventArgs _)
        {
            if (!IsEditable) return;
            windowManager.Drag(WindowKey.BuffOverlay);
        }

        private IEnumerable<TimedItemState> OrderedItems
        {
            get
            {
                var items = buffTracker.Items.AsEnumerable();
                items = settings.Order == OverlayOrderMode.Ascending
                    ? items.OrderBy(i => i.TimeLeft)
                    : items.OrderByDescending(i => i.TimeLeft);
                return items;
            }
        }

        private string ContainerStyle
        {
            get
            {
                if (!IsEditable) return string.Empty;

                var culture = System.Globalization.CultureInfo.InvariantCulture;
                var minHeight = settings.IconSize + 5;
                var minWidth = (settings.IconSize + 5) * 10;
                return $"min-height: {minHeight.ToString(culture)}px; min-width: {minWidth.ToString(culture)}px;";
            }
        }

        private string TrackStyle => "gap: 4px;";

        private string IconStyle =>
            $"width: {settings.IconSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}px; " +
            $"height: {settings.IconSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;";

        private static string FormatTimeLeft(TimeSpan timeLeft)
        {
            var seconds = Math.Max(0, timeLeft.TotalSeconds);
            return seconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        }


        public void Dispose()
        {
            buffTracker.StateChanged -= OnLiveStateChanged;
            appSettingsService.SettingsChanged -= OnSettingsChanged;
        }
    }
}
