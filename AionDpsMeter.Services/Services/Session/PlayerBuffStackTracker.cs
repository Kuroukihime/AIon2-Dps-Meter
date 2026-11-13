using AionDpsMeter.Core.Models;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class PlayerBuffStackTracker : IPlayerBuffStackTracker, IDisposable
    {
        private sealed class CastEntry
        {
            public required int CastId { get; init; }
            public required DateTime AppliedAtUtc { get; init; }
            public DateTime LastUpdatedAtUtc { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
        }

        private sealed class BuffEntry
        {
            public required int BuffId { get; init; }
            public required int Threshold { get; init; }
            public string BuffName { get; set; } = string.Empty;
            public string BuffIcon { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public Dictionary<int, CastEntry> Casts { get; } = new();
        }

        private readonly Lock trackerLock = new();
        private readonly Dictionary<int, BuffEntry> trackedBuffs = new();
        private readonly Dictionary<int, int> thresholdsByBuffId = new()
        {
            [13000001] = 5
        };

        private readonly Timer cleanupTimer;

        public event EventHandler<int>? BuffActivated;
        public event EventHandler<int>? BuffDeactivated;
        public event EventHandler? StateChanged;

        public PlayerBuffStackTracker()
        {
            cleanupTimer = new Timer(_ => Tick(), null,
                TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }

        public IReadOnlyList<BuffStackState> BuffsWithThreshold
        {
            get
            {
                lock (trackerLock)
                {
                    var now = DateTime.UtcNow;
                    return trackedBuffs.Values
                        .Select(b => BuildState(b, now))
                        .ToList();
                }
            }
        }

        public IReadOnlyList<BuffStackState> BuffsAtOrAboveThreshold
        {
            get
            {
                lock (trackerLock)
                {
                    var now = DateTime.UtcNow;
                    return trackedBuffs.Values
                        .Where(b => b.Casts.Count(c => c.Value.ExpiresAtUtc > now) >= b.Threshold)
                        .Select(b => BuildState(b, now))
                        .ToList();
                }
            }
        }

        public void Register(BuffEvent buffEvent)
        {
            ArgumentNullException.ThrowIfNull(buffEvent);

            Result result;
            lock (trackerLock)
            {
                if (!thresholdsByBuffId.TryGetValue(buffEvent.BuffId, out var threshold))
                {
                    return;
                }

                var now = DateTime.UtcNow;

                if (!trackedBuffs.TryGetValue(buffEvent.BuffId, out var buff))
                {
                    buff = new BuffEntry { BuffId = buffEvent.BuffId, Threshold = threshold };
                    trackedBuffs[buffEvent.BuffId] = buff;
                }

                buff.BuffName = buffEvent.BuffName;
                buff.BuffIcon = buffEvent.BuffIcon ?? string.Empty;

                var duration = TimeSpan.FromMilliseconds(buffEvent.DurationMs);

                if (buff.Casts.TryGetValue(buffEvent.CastId, out var cast) && cast.ExpiresAtUtc > now)
                {
                    cast.ExpiresAtUtc = cast.AppliedAtUtc + duration;
                    cast.LastUpdatedAtUtc = now;
                }
                else
                {
                    buff.Casts[buffEvent.CastId] = new CastEntry
                    {
                        CastId = buffEvent.CastId,
                        AppliedAtUtc = now,
                        LastUpdatedAtUtc = now,
                        ExpiresAtUtc = now + duration,
                    };
                }

                result = Evaluate(now, changed: true);
            }

            Raise(result);
        }

        public void RemoveByCastId(int castId)
        {
            Result result;
            lock (trackerLock)
            {
                var removed = false;
                foreach (var buff in trackedBuffs.Values)
                {
                    removed |= buff.Casts.Remove(castId);
                }

                result = Evaluate(DateTime.UtcNow, removed);
            }

            Raise(result);
        }

        public void Clear()
        {
            Result result;
            lock (trackerLock)
            {
                var deactivated = trackedBuffs.Values
                    .Where(b => b.IsActive)
                    .Select(b => b.BuffId)
                    .ToList();

                result = new Result(new List<int>(), deactivated, trackedBuffs.Count > 0);
                trackedBuffs.Clear();
            }

            Raise(result);
        }

        private void Tick()
        {
            Result result;
            lock (trackerLock)
            {
                result = Evaluate(DateTime.UtcNow, changed: false);
            }

            Raise(result);
        }

        private readonly record struct Result(List<int> Activated, List<int> Deactivated, bool Changed);

        private Result Evaluate(DateTime now, bool changed)
        {
            var activated = new List<int>();
            var deactivated = new List<int>();
            var emptyBuffs = new List<int>();

            foreach (var buff in trackedBuffs.Values)
            {
                var expired = buff.Casts.Values
                    .Where(c => c.ExpiresAtUtc <= now)
                    .Select(c => c.CastId)
                    .ToList();

                foreach (var id in expired)
                {
                    buff.Casts.Remove(id);
                }

                if (expired.Count > 0)
                {
                    changed = true;
                }

                var shouldBeActive = buff.Casts.Count >= buff.Threshold;
                if (shouldBeActive != buff.IsActive)
                {
                    buff.IsActive = shouldBeActive;
                    (shouldBeActive ? activated : deactivated).Add(buff.BuffId);
                    changed = true;
                }

                if (buff.Casts.Count == 0)
                {
                    emptyBuffs.Add(buff.BuffId);
                }
            }

            foreach (var id in emptyBuffs)
            {
                trackedBuffs.Remove(id);
            }

            return new Result(activated, deactivated, changed);
        }

        private void Raise(Result r)
        {
            foreach (var id in r.Activated)
            {
                BuffActivated?.Invoke(this, id);
            }

            foreach (var id in r.Deactivated)
            {
                BuffDeactivated?.Invoke(this, id);
            }

            if (r.Changed)
            {
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static BuffStackState BuildState(BuffEntry buff, DateTime now)
        {
            var casts = buff.Casts.Values
                .Where(c => c.ExpiresAtUtc > now)
                .Select(c => new BuffStackCastState
                {
                    CastId = c.CastId,
                    AppliedAtUtc = c.AppliedAtUtc,
                    LastUpdatedAtUtc = c.LastUpdatedAtUtc,
                    TotalDuration = c.ExpiresAtUtc - c.AppliedAtUtc,
                    TimeLeft = c.ExpiresAtUtc - now,
                })
                .OrderByDescending(c => c.TimeLeft)
                .ToList();

            return new BuffStackState
            {
                BuffId = buff.BuffId,
                BuffName = buff.BuffName,
                BuffIcon = buff.BuffIcon,
                Threshold = buff.Threshold,
                StackCount = casts.Count,
                IsThresholdReached = casts.Count >= buff.Threshold,
                Casts = casts,
            };
        }

        public void Dispose() => cleanupTimer.Dispose();
    }
}