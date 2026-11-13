using AionDpsMeter.Core.Models;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class BuffStackCastState
    {
        public required int CastId { get; init; }
        public required DateTime AppliedAtUtc { get; init; }
        public required DateTime LastUpdatedAtUtc { get; init; }
        public required TimeSpan TotalDuration { get; init; }
        public required TimeSpan TimeLeft { get; init; }
    }

    public sealed class BuffStackState
    {
        public required int BuffId { get; init; }
        public required string BuffName { get; init; }
        public required string BuffIcon { get; init; }
        public required int Threshold { get; init; }
        public required int StackCount { get; init; }
        public required bool IsThresholdReached { get; init; }
        public required IReadOnlyList<BuffStackCastState> Casts { get; init; }
    }

    public interface IPlayerBuffStackTracker
    {
        event EventHandler<int>? BuffActivated;
        event EventHandler<int>? BuffDeactivated;
        event EventHandler? StateChanged;

        IReadOnlyList<BuffStackState> BuffsWithThreshold { get; }
        IReadOnlyList<BuffStackState> BuffsAtOrAboveThreshold { get; }

        void Register(BuffEvent buffEvent);
        void RemoveByCastId(int castId);
        void Clear();
    }
}
