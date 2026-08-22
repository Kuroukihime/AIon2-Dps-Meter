namespace AionDpsMeter.Services.Models
{
    public sealed class PlayerStatSnapshot
    {
        public DateTime CapturedAt { get; init; }
        public PlayerCharacterStats Stats { get; init; } = new(new Dictionary<ushort, int>());

        public static PlayerStatSnapshot Create(PlayerCharacterStats stats, DateTime? capturedAt = null)
        {
            ArgumentNullException.ThrowIfNull(stats);
            return new PlayerStatSnapshot
            {
                CapturedAt = capturedAt ?? DateTime.Now,
                Stats = stats,
            };
        }
    }
}
