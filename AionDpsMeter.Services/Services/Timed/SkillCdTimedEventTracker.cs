using AionDpsMeter.Services.Services.Settings;

namespace AionDpsMeter.Services.Services.Timed
{
  
    public class SkillCdTimedEventTracker : TimedEventTracker
    {
        private HashSet<uint> _trackedIds = [];
        private bool _enabled;

        public SkillCdTimedEventTracker(IAppSettingsService appSettingsService, TimeSpan? tickInterval = null)
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
            var overlaySettings = AppSettingsService.SkillCdOverlaySettings;

            _enabled = overlaySettings.Enabled;
            _trackedIds = overlaySettings.TrackedIdList
                .Select(id => unchecked((uint)id))
                .ToHashSet();
        }
    }
}
