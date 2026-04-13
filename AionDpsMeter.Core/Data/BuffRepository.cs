using AionDpsMeter.Core.Models;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AionDpsMeter.Core.Data
{
    public class BuffRepository
    {
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
            if (raw is not null) buffsById = raw.ToFrozenDictionary();
        }

        public BuffData? GetBuff(int buffId) => buffsById.GetValueOrDefault(buffId);

        public bool IsBuff(int buffId) => buffsById.TryGetValue(buffId, out var buff) && buff.Type == BuffType.BUFF;
    }
}
