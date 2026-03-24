namespace AionDpsMeter.Services.Models
{
    public sealed class ActiveTargetInfo
    {
        public int TargetId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int HpTotal { get; init; }
        public int HpCurrent { get; init; }
    }
}
