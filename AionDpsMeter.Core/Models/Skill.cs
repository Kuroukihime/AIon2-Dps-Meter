namespace AionDpsMeter.Core.Models
{
    public sealed class Skill
    {
        public int Id { get; init; }
        public int ClassId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Icon { get; init; }
        public bool[] SpecializationFlags { get; init; } = [];

        /// <summary>True when this skill has an associated icon path.</summary>
        public bool HasIcon => !string.IsNullOrEmpty(Icon);

        public override bool Equals(object? obj) => obj is Skill skill && Id == skill.Id;
        public override int GetHashCode() => Id.GetHashCode();
    }
}
