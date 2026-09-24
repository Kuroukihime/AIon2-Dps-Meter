using AionDpsMeter.Core.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Timed;
using AionDpsMeter.UI.Services.Windowing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;


namespace AionDpsMeter.UI.Pages
{
    partial class SkillCdOverlay(IWindowManagerService windowManager, WindowHelper windowHelper, IAppSettingsService appSettingsService, [FromKeyedServices("SkillCd")] ITimedEventTracker skillCdTracker)
    {

        private bool IsEditable => windowHelper.IsSkillCdEdit;

        private OverlaySettings settings = new();

        protected override void OnInitialized()
        {
            settings = appSettingsService.SkillCdOverlaySettings;

            skillCdTracker.StateChanged += OnLiveStateChanged;
            appSettingsService.SettingsChanged += OnSettingsChanged;
        }

        private void OnLiveStateChanged(object? sender, EventArgs e) =>
            InvokeAsync(StateHasChanged);

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            settings = appSettingsService.SkillCdOverlaySettings;
            InvokeAsync(StateHasChanged);
        }

        private void BeginDrag(MouseEventArgs _)
        {
            if (!IsEditable) return;
            windowManager.Drag(WindowKey.SkillCdOverlay);
        }

        private IEnumerable<TimedItemState> OrderedItems
        {
            get
            {
                var items = skillCdTracker.Items.AsEnumerable();
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
            skillCdTracker.StateChanged -= OnLiveStateChanged;
            appSettingsService.SettingsChanged -= OnSettingsChanged;
        }
    }
}
