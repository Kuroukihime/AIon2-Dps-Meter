using AionDpsMeter.Services.Models;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed class BuffStatsViewModel : ViewModelBase
    {
        private readonly BuffStats _stats;

        public BuffStatsViewModel(BuffStats stats)
        {
            _stats = stats;
        }

        public int    BuffId                => _stats.BuffId;
        public string BuffName              => _stats.BuffName;
        public string? BuffIcon             => _stats.BuffIcon;
        public bool   HasBuffIcon           => !string.IsNullOrEmpty(_stats.BuffIcon);
        public string Description           => _stats.Description;
        public int    ApplicationCount      => _stats.ApplicationCount;
        public string ApplicationCountText  => $"x{_stats.ApplicationCount}";
        public string DurationFormatted     => _stats.DurationFormatted;
        public double EffectiveDurationSec  => _stats.EffectiveDurationSeconds;


        public string TooltipText
        {
            get
            {
                var desc = string.IsNullOrWhiteSpace(_stats.Description)
                    ? _stats.BuffName
                    : _stats.Description;
                return $"{desc}\nUptime: {_stats.DurationFormatted}  (×{_stats.ApplicationCount})";
            }
        }
    }
}
