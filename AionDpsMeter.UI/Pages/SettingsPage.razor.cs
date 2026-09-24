using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.UI.Services.Windowing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AionDpsMeter.UI.Pages
{
    partial class SettingsPage(IAppSettingsService settings, WindowHelper windowHelper, IWindowManagerService windowManager) : IDisposable
    {
        private List<Skill> AllSkills  => FilteredSkills();

        private int _windowOpacityPercent;
        private double _playerRowScale;
        private bool _capturingHotkey;
        private bool _developerExpanded;
        private string _activeGroup = "appearance";
        private string _version = "1.0.0";

       
        private const int MaxTrackedBuffSkills = 10;
        private const int MaxTrackedCdSkills = 10;
        private bool _buffPickerOpen;
        private bool _skillCdPickerOpen;
        private string _skillSearchQuery = string.Empty;
        private string _cdSkillSearchQuery = string.Empty;
        private bool _hasOpenedOverlaysTab;

        private sealed record SettingsGroup(string Id, string Label, string Icon);
        private sealed record UiStyleOption(int Value, string Label, string PreviewClass);
        private sealed record OrderOption(OverlayOrderMode Value, string Label);

        private readonly List<SettingsGroup> _groups = new()
    {
        new("appearance", "Appearance", "&#9707;"),
        new("hotkeys", "Hotkeys", "&#9000;"),
        new("tracking", "Tracking", "&#9881;"),
        new("overlays", "[BETA] Overlays", "&#9635;"),
    };

        private readonly List<UiStyleOption> _uiStyles = new()
    {
        new(1, "Standard", "standard"),
        new(2, "Compact", "compact"),
    };

        private readonly List<OrderOption> _orderOptions = new()
    {
        new(OverlayOrderMode.Ascending, "Least time"),
        new(OverlayOrderMode.Descending, "Most time"),
    };

        protected override void OnInitialized()
        {
            _windowOpacityPercent = (int)Math.Round(settings.WindowOpacity * 100);
            _playerRowScale = settings.PlayerRowScale;

            var asmVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
            if (asmVersion is not null)
                _version = $"{asmVersion.Major}.{asmVersion.Minor}.{asmVersion.Build}";

        }



        private void SelectGroup(string id)
        {
            _activeGroup = id;

            if (id == "overlays" && !_hasOpenedOverlaysTab)
            {
                _hasOpenedOverlaysTab = true;
            }
        }

        private void CloseWindow()
        {
            windowHelper.CloseSettings();
        }


        private List<Skill> FilteredSkills()
        {
           return  GameDataProvider.Instance.Skills.Skills
               .Where(r=>r.Key > 10000000).Select(r => r.Value).ToList();
        }

        public void Dispose()
        {
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

        private void BeginDrag(MouseEventArgs _) => windowManager.Drag(WindowKey.Settings);

  

        private void SetBuffOverlayEnabled(bool value)
        {
            var s = settings.BufOverlaySettings;
            s.Enabled = value;
            settings.BufOverlaySettings = s;
        }

        private void SetSkillOverlayEnabled(bool value)
        {
            var s = settings.SkillCdOverlaySettings;
            s.Enabled = value;
            settings.SkillCdOverlaySettings = s;
        }

        private void SetBuffOverlayOrder(OverlayOrderMode mode)
        {
            var s = settings.BufOverlaySettings;
            s.Order = mode;
            settings.BufOverlaySettings = s;
        }

        private void SetSkillOverlayOrder(OverlayOrderMode mode)
        {
            var s = settings.SkillCdOverlaySettings;
            s.Order = mode;
            settings.SkillCdOverlaySettings = s;
        }

        private void OnBuffOverlayIconSizeInput(ChangeEventArgs e)
        {
            if (!double.TryParse(e.Value?.ToString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var size)) return;

            var s = settings.BufOverlaySettings;
            s.IconSize = Math.Clamp(size, 20, 50);
            settings.BufOverlaySettings = s;
        }

        private void OnSkillIconSizeInput(ChangeEventArgs e)
        {
            if (!double.TryParse(e.Value?.ToString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var size)) return;

            var s = settings.SkillCdOverlaySettings;
            s.IconSize = Math.Clamp(size, 20, 50);
            settings.SkillCdOverlaySettings = s;
        }

        private List<Skill> TrackedBuffSkills =>
            settings.BufOverlaySettings.TrackedIdList
                .Select(id => AllSkills.FirstOrDefault(sk => sk.Id == id))
                .Where(sk => sk is not null)
                .Select(sk => sk!)
                .ToList();

        private List<Skill> TrackedCdSkills =>
            settings.SkillCdOverlaySettings.TrackedIdList
                .Select(id => AllSkills.FirstOrDefault(sk => sk.Id == id))
                .Where(sk => sk is not null)
                .Select(sk => sk!)
                .ToList();

        private bool CanAddMoreBuffSkills => settings.BufOverlaySettings.TrackedIdList.Count < MaxTrackedBuffSkills;

        private bool CanAddMoreCdSkills => settings.SkillCdOverlaySettings.TrackedIdList.Count < MaxTrackedCdSkills;

        private IEnumerable<Skill> FilteredSkillResults
        {
            get
            {
                var query = _skillSearchQuery.Trim();
                var tracked = settings.BufOverlaySettings.TrackedIdList;

                IEnumerable<Skill> source = AllSkills.Where(sk => !tracked.Contains(sk.Id));

                if (!string.IsNullOrEmpty(query))
                    source = source.Where(sk => sk.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

                return source.Take(50);
            }
        }
        private IEnumerable<Skill> FilteredSkillCdResults
        {
            get
            {
                var query = _cdSkillSearchQuery.Trim();
                var tracked = settings.SkillCdOverlaySettings.TrackedIdList;

                IEnumerable<Skill> source = AllSkills.Where(sk => !tracked.Contains(sk.Id));

                if (!string.IsNullOrEmpty(query))
                    source = source.Where(sk => sk.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

                return source.Take(50);
            }
        }

        private void OpenBuffPicker()
        {
            if (!CanAddMoreBuffSkills) return;
            _skillSearchQuery = string.Empty;
            _buffPickerOpen = true;
        }

        private void OpenSkillCdPicker()
        {
            if (!CanAddMoreCdSkills) return;
            _cdSkillSearchQuery = string.Empty;
            _skillCdPickerOpen = true;
        }

        private void CloseBuffPicker() => _buffPickerOpen = false;
        private void CloseSkillCdPicker() => _skillCdPickerOpen = false;

        private void OnSkillSearchInput(ChangeEventArgs e) => _skillSearchQuery = e.Value?.ToString() ?? string.Empty;

        private void OnCdSkillSearchInput(ChangeEventArgs e) => _cdSkillSearchQuery = e.Value?.ToString() ?? string.Empty;

        private void AddTrackedBuffSkill(int skillId)
        {
            var s = settings.BufOverlaySettings;
            if (s.TrackedIdList.Count >= MaxTrackedBuffSkills) return;
            if (s.TrackedIdList.Contains(skillId)) return;

            s.TrackedIdList.Add(skillId);
            settings.BufOverlaySettings = s;
            _buffPickerOpen = false;
        }

        private void AddTrackedCdSkill(int skillId)
        {
            var s = settings.SkillCdOverlaySettings;
            if (s.TrackedIdList.Count >= MaxTrackedCdSkills) return;
            if (s.TrackedIdList.Contains(skillId)) return;

            s.TrackedIdList.Add(skillId);
            settings.SkillCdOverlaySettings = s;
            _buffPickerOpen = false;
        }

        private void RemoveTrackedBuffSkill(int skillId)
        {
            var s = settings.BufOverlaySettings;
            s.TrackedIdList.Remove(skillId);
            settings.BufOverlaySettings = s;
        }

        private void RemoveTrackedCdSkill(int skillId)
        {
            var s = settings.SkillCdOverlaySettings;
            s.TrackedIdList.Remove(skillId);
            settings.SkillCdOverlaySettings = s;
        }

    }
}