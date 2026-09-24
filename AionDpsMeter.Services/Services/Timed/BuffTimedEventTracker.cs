using AionDpsMeter.Services.Services.Settings;

namespace AionDpsMeter.Services.Services.Timed
{
  
    public class BuffTimedEventTracker : TimedEventTracker
    {
        private HashSet<uint> _trackedIds = [];
        private bool _enabled;

        public BuffTimedEventTracker(IAppSettingsService appSettingsService, TimeSpan? tickInterval = null)
            : base(appSettingsService, tickInterval)
        {
            LoadFromSettings();
        }

        protected override bool ShouldTrack(TimedEvent evt)
        {
            return _enabled && _trackedIds.Contains(evt.Id);
        }

        protected override void OnSettingsChanged()
        {
            LoadFromSettings();
            base.OnSettingsChanged();
        }

        private void LoadFromSettings()
        {
            var overlaySettings = AppSettingsService.BufOverlaySettings;

            _enabled = overlaySettings.Enabled;
            _trackedIds = overlaySettings.TrackedIdList
                .Select(id => unchecked((uint)id))
                .ToHashSet();
        }
    }
}
