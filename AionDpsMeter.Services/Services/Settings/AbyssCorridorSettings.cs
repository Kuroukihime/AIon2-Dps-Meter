using System.Text.Json.Serialization;

namespace AionDpsMeter.Services.Services.Settings
{
    public sealed class AbyssCorridorSettings
    {
        public const int MinLeadMinutes = 1;
        public const int MaxLeadMinutes = 60;

        [JsonPropertyName("showTimer")]
        public bool ShowTimer { get; set; } = true;

        [JsonPropertyName("trayNotification")]
        public bool TrayNotification { get; set; } = true;

        [JsonPropertyName("sound")]
        public bool Sound { get; set; } = true;

        [JsonPropertyName("leadMinutes")]
        public int LeadMinutes { get; set; } = 5;
    }
}
