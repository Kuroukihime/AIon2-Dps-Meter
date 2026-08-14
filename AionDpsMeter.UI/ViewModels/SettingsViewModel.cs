using AionDpsMeter.Services.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

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
        private bool _bossOnlyCapture;

        [ObservableProperty]
        private int _historyRetantionPeriod;

        [ObservableProperty]
        private int _windowOpacityPercent;

        [ObservableProperty]
        private double _playerRowScale;

        [ObservableProperty]
        private string? _backgroundImagePath;

        [ObservableProperty]
        private bool _relativeProgressBar;

        [ObservableProperty]
        private bool _groupSummonDamage;

        [ObservableProperty]
        private bool _showPlayerDeaths;

        [ObservableProperty]
        private string _toggleVisibilityHotkey = string.Empty;

        [ObservableProperty]
        private int _uiStyle;

        public SettingsViewModel(IAppSettingsService settingsService)
        {
            _settingsService = settingsService;
            _isPacketLoggingEnabled = settingsService.IsPacketLoggingEnabled;
            _isNicknameHidden = settingsService.IsNicknameHidden;
            _bossOnlyCapture = settingsService.BossOnlyCapture;
            _historyRetantionPeriod = settingsService.HistoryRetantionPeriod;
            _windowOpacityPercent = (int)Math.Round(settingsService.WindowOpacity * 100);
            _playerRowScale = settingsService.PlayerRowScale;
            _backgroundImagePath = settingsService.BackgroundImagePath;
            _relativeProgressBar = settingsService.RelativeProgressBar;
            _toggleVisibilityHotkey = settingsService.ToggleVisibilityHotkey;
            _groupSummonDamage = settingsService.GroupSummonDamage;
            _showPlayerDeaths = settingsService.ShowPlayerDeaths;
            _uiStyle = settingsService.UiStyle;
        }

        partial void OnUiStyleChanged(int value)
        {
            _settingsService.UiStyle = value;
        }

        partial void OnIsPacketLoggingEnabledChanged(bool value)
        {
            _settingsService.IsPacketLoggingEnabled = value;
        }

        partial void OnIsNicknameHiddenChanged(bool value)
        {
            _settingsService.IsNicknameHidden = value;
        }

        partial void OnBossOnlyCaptureChanged(bool value)
        {
            _settingsService.BossOnlyCapture = value;
        }

        partial void OnHistoryRetantionPeriodChanged(int value)
        {
            _settingsService.HistoryRetantionPeriod = Math.Clamp(value, 1, 9999);
        }

        partial void OnWindowOpacityPercentChanged(int value)
        {
            int clamped = Math.Clamp(value, 10, 100);
            _settingsService.WindowOpacity = clamped / 100.0;
        }

        partial void OnPlayerRowScaleChanged(double value)
        {
            double clamped = Math.Clamp(value, 0.5, 3);
            _settingsService.PlayerRowScale = clamped;
        }

        partial void OnBackgroundImagePathChanged(string? value)
        {
            _settingsService.BackgroundImagePath = value;
        }

        partial void OnRelativeProgressBarChanged(bool value)
        {
            _settingsService.RelativeProgressBar = value;
        }
        
        partial void OnGroupSummonDamageChanged(bool value)
        {
            _settingsService.GroupSummonDamage = value;
        }

        partial void OnShowPlayerDeathsChanged(bool value)
        {
            _settingsService.ShowPlayerDeaths = value;
        }

        partial void OnToggleVisibilityHotkeyChanged(string value)
        {
            _settingsService.ToggleVisibilityHotkey = value;
        }

        [RelayCommand]
        private void PickBackgroundImage()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Background Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All Files|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
                BackgroundImagePath = dialog.FileName;
        }

        [RelayCommand]
        private void ResetBackgroundImage()
        {
            BackgroundImagePath = null;
        }
    }
}
