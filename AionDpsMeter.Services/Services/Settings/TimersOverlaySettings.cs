using System.Text.Json.Serialization;

namespace AionDpsMeter.Services.Services.Settings
{
    public sealed class TimersOverlaySettings
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("showClock")]
        public bool ShowClock { get; set; } = true;

        [JsonPropertyName("use24HourClock")]
        public bool Use24HourClock { get; set; } = true;
    }
}
