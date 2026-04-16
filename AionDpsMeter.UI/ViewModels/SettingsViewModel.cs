using AionDpsMeter.Services.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class SettingsViewModel : ViewModelBase
    {
        private readonly IAppSettingsService _settingsService;

        [ObservableProperty]
        private bool _isPacketLoggingEnabled;

        [ObservableProperty]
        private bool _isNicknameHidden;

        [ObservableProperty]
        private int _historyDamageThreshold;

        [ObservableProperty]
        private int _windowOpacityPercent;

        public SettingsViewModel(IAppSettingsService settingsService)
        {
            _settingsService = settingsService;
            _isPacketLoggingEnabled = settingsService.IsPacketLoggingEnabled;
            _isNicknameHidden = settingsService.IsNicknameHidden;
            _historyDamageThreshold = settingsService.HistoryDamageThreshold;
            _windowOpacityPercent = (int)Math.Round(settingsService.WindowOpacity * 100);
        }

        partial void OnIsPacketLoggingEnabledChanged(bool value)
        {
            _settingsService.IsPacketLoggingEnabled = value;
        }

        partial void OnIsNicknameHiddenChanged(bool value)
        {
            _settingsService.IsNicknameHidden = value;
        }

        partial void OnHistoryDamageThresholdChanged(int value)
        {
            _settingsService.HistoryDamageThreshold = Math.Clamp(value, 0, int.MaxValue);
        }

        partial void OnWindowOpacityPercentChanged(int value)
        {
            int clamped = Math.Clamp(value, 10, 100);
            _settingsService.WindowOpacity = clamped / 100.0;
        }
    }
}
