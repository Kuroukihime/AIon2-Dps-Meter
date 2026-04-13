namespace AionDpsMeter.Core.Models
{
 
    public sealed class BuffEvent
    {
        public int EntityId { get; init; }
        public int BuffId { get; init; }
        public string BuffName { get; init; } = string.Empty;
        public string? BuffIcon { get; init; }
        public string Description { get; init; } = string.Empty;
        public uint DurationMs { get; init; }
        public DateTime AppliedAt { get; init; }
        public int CasterId { get; init; }
    }
}
