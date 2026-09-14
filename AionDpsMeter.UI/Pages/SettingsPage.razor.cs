using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Services.UiCommands;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AionDpsMeter.UI.Pages
{
    partial class SettingsPage (IAppSettingsService settings, IUiCommandService uiCommands)
    {
        private int _windowOpacityPercent;
        private double _playerRowScale;
        private bool _capturingHotkey;
        private bool _developerExpanded;
        private string _activeGroup = "appearance";
        private string _version = "1.8.1";

        private sealed record SettingsGroup(string Id, string Label, string Icon);
        private sealed record UiStyleOption(int Value, string Label, string PreviewClass);

        private readonly List<SettingsGroup> _groups = new()
    {
        new("appearance", "Appearance", "&#9707;"),
        new("hotkeys", "Hotkeys", "&#9000;"),
        new("tracking", "Tracking", "&#9881;"),
    };

        private readonly List<UiStyleOption> _uiStyles = new()
    {
        new(1, "Standard", "standard"),
        new(2, "Compact", "compact"),
    };

        protected override void OnInitialized()
        {
            _windowOpacityPercent = (int)Math.Round(settings.WindowOpacity * 100);
            _playerRowScale = settings.PlayerRowScale;

            var asmVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
            if (asmVersion is not null)
                _version = $"{asmVersion.Major}.{asmVersion.Minor}.{asmVersion.Build}";

        }

      

        private void SelectGroup(string id) => _activeGroup = id;

        private void CloseWindow()
        {
            uiCommands.Request(new UiCommandRequest(UiCommandType.CloseSettings));
        }

        private void ToggleDeveloperSection() => _developerExpanded = !_developerExpanded;

        private void OnWindowOpacityInput(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var pct))
            {
                _windowOpacityPercent = pct;
                settings.WindowOpacity = pct / 100.0;
            }
        }

        private void OnPlayerRowScaleInput(ChangeEventArgs e)
        {
            if (double.TryParse(e.Value?.ToString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var scale))
            {
                _playerRowScale = scale;
                settings.PlayerRowScale = scale;
            }
        }

        private void SetUiStyle(int value) => settings.UiStyle = value;

        private void OnRetentionPeriodInput(ChangeEventArgs e)
        {
            var raw = e.Value?.ToString() ?? string.Empty;
            var digitsOnly = new string(raw.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length == 0) return;
            if (int.TryParse(digitsOnly, out var days))
                settings.HistoryRetantionPeriod = Math.Clamp(days, 1, 9999);
        }

        private void OnHotkeyFocus(FocusEventArgs e) => _capturingHotkey = true;

        private void OnHotkeyBlur(FocusEventArgs e) => _capturingHotkey = false;

        private static readonly HashSet<string> IgnoredKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shift", "Control", "Alt", "Meta", "Tab", "Escape"
    };

        private void OnHotkeyKeyDown(KeyboardEventArgs e)
        {
            if (IgnoredKeys.Contains(e.Key)) return;

            var parts = new List<string>();
            if (e.CtrlKey) parts.Add("Ctrl");
            if (e.ShiftKey) parts.Add("Shift");
            if (e.AltKey) parts.Add("Alt");

            var keyName = e.Key.Length == 1 ? e.Key.ToUpperInvariant() : e.Key;
            parts.Add(keyName);

            settings.ToggleVisibilityHotkey = string.Join("+", parts);
            _capturingHotkey = false;
        }

    }
}
