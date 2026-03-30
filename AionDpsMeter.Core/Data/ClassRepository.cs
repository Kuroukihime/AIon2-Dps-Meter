using AionDpsMeter.Core.Models;
using System.Text.Json;

namespace AionDpsMeter.Core.Data
{
    /// <summary>
    /// Owns all character-class data: loading from JSON and class lookups.
    /// </summary>
    public sealed class ClassRepository
    {
        private readonly Dictionary<int, CharacterClass> _classesById = [];

        // ---------------------------------------------------------------------------
        // Loading
        // ---------------------------------------------------------------------------

        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Classes data file not found: {path}");

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var classesFile = JsonSerializer.Deserialize<ClassesFile>(json, options)
                ?? throw new InvalidDataException("Failed to deserialize classes.json");

            foreach (var classData in classesFile.Classes)
            {
                _classesById[classData.Id] = new CharacterClass
                {
                    Id = classData.Id,
                    Name = classData.Name,
                    Icon = classData.Icon
                };
            }
        }

        // ---------------------------------------------------------------------------
        // Public query API
        // ---------------------------------------------------------------------------

        public CharacterClass? GetById(int classId) =>
            _classesById.TryGetValue(classId, out var c) ? c : null;

        /// <summary>
        /// Resolves the class from a full skill code.
        /// The class id is encoded as the first 2 digits of the skill code.
        /// </summary>
        public CharacterClass? GetBySkillCode(int skillCode)
        {
            var s = skillCode.ToString();
            if (s.Length < 2)
                return null;

            if (!int.TryParse(s[..2], out var classId))
                return null;

            return GetById(classId);
        }

        public CharacterClass GetOrDefault(int classId)
        {
            if (_classesById.TryGetValue(classId, out var c))
                return c;

            return new CharacterClass
            {
                Id = classId,
                Name = $"Unknown Class ({classId})",
                Icon = null
            };
        }

        public IEnumerable<CharacterClass> GetAll() => _classesById.Values;
    }
}
