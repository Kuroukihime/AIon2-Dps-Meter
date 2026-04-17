using System.Text.Json.Serialization;

namespace AionDpsMeter.Core.Models
{
    public sealed class ClassData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
    }

    public sealed class ClassesFile
    {
        public List<ClassData> Classes { get; set; } = [];
    }

    public sealed class MobData
    {
        public string Name { get; set; } = string.Empty;
        public bool IsBoss { get; set; }
    }


    public enum BuffType
    {
        PASSIVE = 0,
        DEBUFF = 1,
        BUFF = 2,
    }

    public sealed class BuffData
    {
        public string Name { get; set; } = string.Empty;
        public BuffType Type { get; set; }
        public string IconUrlPart { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>Fully resolved icon URL, set during repository load.</summary>
        [JsonIgnore]
        public string? Icon { get; set; }
    }

    internal sealed class SkillsData
    {
        [JsonPropertyName("skillCodeOffsets")]
        public int[]? SkillCodeOffsets { get; set; }

        [JsonPropertyName("skills")]
        public Dictionary<string, string> Skills { get; set; } = [];
    }

}
