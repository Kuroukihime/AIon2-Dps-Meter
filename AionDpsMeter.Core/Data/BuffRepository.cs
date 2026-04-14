using AionDpsMeter.Core.Models;
using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AionDpsMeter.Core.Data
{
    public class BuffRepository
    {
        private const string BaseUrl = "https://assets.playnccdn.com/static-aion2-gamedata/resources";

        private FrozenDictionary<int, BuffData> buffsById = FrozenDictionary<int, BuffData>.Empty;

        public void Load(string path)
        {
            if (!File.Exists(path))
                return;
            var json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var raw = JsonSerializer.Deserialize<Dictionary<int, BuffData>>(json, options);
            if (raw is not null)
            {
                foreach (var buff in raw.Values)
                {
                    buff.Icon = ResolveIconUrl(buff.IconUrlPart);
                }
                buffsById = raw.ToFrozenDictionary();
            }
        }

        public BuffData? GetBuff(int buffId) => buffsById.GetValueOrDefault(buffId);

        public bool IsBuff(int buffId) => buffsById.TryGetValue(buffId, out var buff) && buff.Type == BuffType.BUFF;

        private static string? ResolveIconUrl(string? iconUrlPart)
        {
            if (string.IsNullOrWhiteSpace(iconUrlPart))
                return null;

            return $"{BaseUrl}/{iconUrlPart}";
        }
    }
}
