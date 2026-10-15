namespace AionDpsMeter.Core.Models
{
    public class SkillCastEvent
    {
        public required int EntityId { get; init; }
        public required int SkillId { get; init; }
        public required string Name { get; init; }
        public required string? IconUrl { get; init; }
        public required TimeSpan Cd { get; init; }
    }
 
}
