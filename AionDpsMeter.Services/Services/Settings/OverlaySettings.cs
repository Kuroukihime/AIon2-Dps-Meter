using System.Text.Json.Serialization;

namespace AionDpsMeter.Services.Services.Settings
{
    public enum OverlayOrderMode
    {
        /// <summary>Least time left first.</summary>
        Ascending,
        /// <summary>Most time left first.</summary>
        Descending
    }

    public sealed class OverlaySettings
    {
        [JsonPropertyName("iconSize")]
        public double IconSize { get; set; } = 40;

        [JsonPropertyName("order")]
        public OverlayOrderMode Order { get; set; } = OverlayOrderMode.Ascending;

        [JsonPropertyName("enabled")] 
        public bool Enabled { get; set; } = false;

        [JsonPropertyName("trackedIdList")]
        public List<int> TrackedIdList { get; set; } = new List<int>();


    }
}
