using AionDpsMeter.Core.Models;
using System.Collections.Frozen;
using System.Text.Json;

namespace AionDpsMeter.Core.Data
{
   
    public sealed class MobRepository
    {
        private FrozenDictionary<int, MobData> mobsById = FrozenDictionary<int, MobData>.Empty;

       

        public void Load(string path)
        {
            if (!File.Exists(path))
                return; 

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = JsonSerializer.Deserialize<Dictionary<int, MobData>>(json, options);
            if (raw is not null)
                mobsById = raw.ToFrozenDictionary();
        }


        public string GetName(int mobId) =>
            mobsById.TryGetValue(mobId, out var mob) ? mob.Name : $"Unknown ({mobId})";

        public bool IsBoss(int mobId) =>
            mobsById.TryGetValue(mobId, out var mob) && mob.IsBoss;
    }
}
