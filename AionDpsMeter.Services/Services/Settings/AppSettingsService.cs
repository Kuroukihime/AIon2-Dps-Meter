using System.Text.Json;
using System.Text.Json.Serialization;

namespace AionDpsMeter.Services.Services.Settings
{
    public sealed class AppSettingsService : IAppSettingsService
    {
        private const string SettingsFilePath = "appsettings.user.json";
        private readonly Lock _lock = new();
        private AppSettingsData _data;

        public event EventHandler? SettingsChanged;

        public AppSettingsService()
        {
            _data = Load();
            _data.HistoryRetantionPeriod = Math.Clamp(_data.HistoryRetantionPeriod, 1, 9999);
        }


        public double PlayerRowScale
        {
            get { lock (_lock) return _data.PlayerRowScale; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.PlayerRowScale != value;
                    _data.PlayerRowScale = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double StatCalcCritChance
        {
            get { lock (_lock) return _data.StatCalcCritChance; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Clamp(value, 0, 1000);
                    changed = _data.StatCalcCritChance != clamped;
                    _data.StatCalcCritChance = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double StatCalcBackAttackRate
        {
            get { lock (_lock) return _data.StatCalcBackAttackRate; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Clamp(value, 0, 1000);
                    changed = _data.StatCalcBackAttackRate != clamped;
                    _data.StatCalcBackAttackRate = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double StatCalcFrontAttackRate
        {
            get { lock (_lock) return _data.StatCalcFrontAttackRate; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Clamp(value, 0, 1000);
                    changed = _data.StatCalcFrontAttackRate != clamped;
                    _data.StatCalcFrontAttackRate = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string StatCalcAttackType
        {
            get { lock (_lock) return _data.StatCalcAttackType; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    var normalized = value is "Front" or "Back" ? value : "None";
                    changed = _data.StatCalcAttackType != normalized;
                    _data.StatCalcAttackType = normalized;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double StatCalcAttackIncreaseCombatPercent
        {
            get { lock (_lock) return _data.StatCalcAttackIncreaseCombatPercent; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Max(0, value);
                    changed = _data.StatCalcAttackIncreaseCombatPercent != clamped;
                    _data.StatCalcAttackIncreaseCombatPercent = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double StatCalcPartyDamageBoost
        {
            get { lock (_lock) return _data.StatCalcPartyDamageBoost; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Max(0, value);
                    changed = _data.StatCalcPartyDamageBoost != clamped;
                    _data.StatCalcPartyDamageBoost = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double StatCalcBossDamageTolerance
        {
            get { lock (_lock) return _data.StatCalcBossDamageTolerance; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Max(0, value);
                    changed = _data.StatCalcBossDamageTolerance != clamped;
                    _data.StatCalcBossDamageTolerance = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double StatCalcPartySmiteBuff
        {
            get { lock (_lock) return _data.StatCalcPartySmiteBuff; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Max(0, value);
                    changed = _data.StatCalcPartySmiteBuff != clamped;
                    _data.StatCalcPartySmiteBuff = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double StatCalcBossSmiteResist
        {
            get { lock (_lock) return _data.StatCalcBossSmiteResist; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Max(0, value);
                    changed = _data.StatCalcBossSmiteResist != clamped;
                    _data.StatCalcBossSmiteResist = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool ShowPlayerDeaths
        {
            get { lock (_lock) return _data.ShowPlayerDeaths; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.ShowPlayerDeaths != value;
                    _data.ShowPlayerDeaths = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsPacketLoggingEnabled
        {
            get { lock (_lock) return _data.IsPacketLoggingEnabled; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.IsPacketLoggingEnabled != value;
                    _data.IsPacketLoggingEnabled = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsNicknameHidden
        {
            get { lock (_lock) return _data.IsNicknameHidden; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.IsNicknameHidden != value;
                    _data.IsNicknameHidden = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int UiStyle
        {
            get { lock (_lock) return _data.UiStyle; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.UiStyle != value;
                    _data.UiStyle = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        public bool BossOnlyCapture
        {
            get { lock (_lock) return _data.BossOnlyCapture; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.BossOnlyCapture != value;
                    _data.BossOnlyCapture = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int HistoryRetantionPeriod
        {
            get { lock (_lock) return _data.HistoryRetantionPeriod; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    int clamped = Math.Clamp(value, 1, 9999);
                    changed = _data.HistoryRetantionPeriod != clamped;
                    _data.HistoryRetantionPeriod = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double WindowOpacity
        {
            get { lock (_lock) return _data.WindowOpacity; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    double clamped = Math.Clamp(value, 0.1, 1.0);
                    changed = _data.WindowOpacity != clamped;
                    _data.WindowOpacity = clamped;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string ToggleVisibilityHotkey
        {
            get { lock (_lock) return _data.ToggleVisibilityHotkey; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.ToggleVisibilityHotkey != value;
                    _data.ToggleVisibilityHotkey = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public double? WindowLeft
        {
            get { lock (_lock) return _data.WindowLeft; }
            set { lock (_lock) { _data.WindowLeft = value; Save(); } }
        }

        public double? WindowTop
        {
            get { lock (_lock) return _data.WindowTop; }
            set { lock (_lock) { _data.WindowTop = value; Save(); } }
        }

        public double? WindowWidth
        {
            get { lock (_lock) return _data.WindowWidth; }
            set { lock (_lock) { _data.WindowWidth = value; Save(); } }
        }

        public double? WindowHeight
        {
            get { lock (_lock) return _data.WindowHeight; }
            set { lock (_lock) { _data.WindowHeight = value; Save(); } }
        }

        public string? BackgroundImagePath
        {
            get { lock (_lock) return _data.BackgroundImagePath; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.BackgroundImagePath != value;
                    _data.BackgroundImagePath = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool RelativeProgressBar
        {
            get { lock (_lock) return _data.RelativeProgressBar; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.RelativeProgressBar != value;
                    _data.RelativeProgressBar = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool GroupSummonDamage
        {
            get { lock (_lock) return _data.GroupSummonDamage; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.GroupSummonDamage != value;
                    _data.GroupSummonDamage = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool OneMinuteDummyMode
        {
            get { lock (_lock) return _data.OneMinuteDummyMode; }
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _data.OneMinuteDummyMode != value;
                    _data.OneMinuteDummyMode = value;
                    if (changed) Save();
                }
                if (changed) SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private AppSettingsData Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettingsData>(json) ?? new AppSettingsData();
                }
            }
            catch { }
            return new AppSettingsData();
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }

        private sealed class AppSettingsData
        {
            [JsonPropertyName("isPacketLoggingEnabled")]
            public bool IsPacketLoggingEnabled { get; set; }

            [JsonPropertyName("isNicknameHidden")]
            public bool IsNicknameHidden { get; set; }

            [JsonPropertyName("bossOnlyCapture")]
            public bool BossOnlyCapture { get; set; }

            [JsonPropertyName("historyRetantionPeriod")]
            public int HistoryRetantionPeriod { get; set; } = 30;

            [JsonPropertyName("windowOpacity")]
            public double WindowOpacity { get; set; } = 0.92;

            [JsonPropertyName("toggleVisibilityHotkey")]
            public string ToggleVisibilityHotkey { get; set; } = "Ctrl+Shift+D";

            [JsonPropertyName("windowLeft")]
            public double? WindowLeft { get; set; }

            [JsonPropertyName("windowTop")]
            public double? WindowTop { get; set; }

            [JsonPropertyName("windowWidth")]
            public double? WindowWidth { get; set; }

            [JsonPropertyName("windowHeight")]
            public double? WindowHeight { get; set; }

            [JsonPropertyName("backgroundImagePath")]
            public string? BackgroundImagePath { get; set; }

            [JsonPropertyName("relativeProgressBar")]
            public bool RelativeProgressBar { get; set; }

            [JsonPropertyName("groupSummonDamage")]
            public bool GroupSummonDamage { get; set; } = true;

            [JsonPropertyName("oneMinuteDummyMode")]
            public bool OneMinuteDummyMode { get; set; }

            [JsonPropertyName("showPlayerDeaths")]
            public bool ShowPlayerDeaths { get; set; } = true;

            [JsonPropertyName("playerRowScale")]
            public double PlayerRowScale { get; set; } = 1.0;

            [JsonPropertyName("uiStyle")]
            public int UiStyle { get; set; } = 1;

            [JsonPropertyName("statCalcCritChance")]
            public double StatCalcCritChance { get; set; } = 80;

            [JsonPropertyName("statCalcBackAttackRate")]
            public double StatCalcBackAttackRate { get; set; } = 80;

            [JsonPropertyName("statCalcFrontAttackRate")]
            public double StatCalcFrontAttackRate { get; set; } = 80;

            [JsonPropertyName("statCalcAttackType")]
            public string StatCalcAttackType { get; set; } = "Back";

            [JsonPropertyName("statCalcAttackIncreaseCombatPercent")]
            public double StatCalcAttackIncreaseCombatPercent { get; set; }

            [JsonPropertyName("statCalcPartyDamageBoost")]
            public double StatCalcPartyDamageBoost { get; set; }

            [JsonPropertyName("statCalcBossDamageTolerance")]
            public double StatCalcBossDamageTolerance { get; set; } = 30;

            [JsonPropertyName("statCalcPartySmiteBuff")]
            public double StatCalcPartySmiteBuff { get; set; }

            [JsonPropertyName("statCalcBossSmiteResist")]
            public double StatCalcBossSmiteResist { get; set; } = 30;
        }
    }
}
