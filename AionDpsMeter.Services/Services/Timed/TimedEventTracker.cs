using AionDpsMeter.Services.Services.Settings;

namespace AionDpsMeter.Services.Services.Timed
{
    public sealed class TimedEvent
    {
        public required uint Id { get; init; }
        public required string Name { get; init; }
        public required string IconUrl { get; init; }
        public required TimeSpan Duration { get; init; }
    }

    public sealed class TimedItemState
    {
        public required uint Id { get; init; }
        public required string Name { get; init; }
        public required string IconUrl { get; init; }
        public required TimeSpan Duration { get; init; }
        public required TimeSpan TimeLeft { get; init; }

        public bool IsExpiring => TimeLeft <= TimeSpan.FromSeconds(2);
    }

    public interface ITimedEventTracker
    {
        void Register(TimedEvent evt);

        void Remove(uint id);

        void Clear();

        IReadOnlyList<TimedItemState> Items { get; }

        event EventHandler? StateChanged;
    }

    public class TimedEventTracker : ITimedEventTracker, IDisposable
    {
        private sealed class TrackedItem
        {
            public required uint Id { get; init; }
            public required string Name { get; set; }
            public required string IconUrl { get; set; }
            public required TimeSpan Duration { get; set; }
            public required DateTime StartUtc { get; set; }
        }

        private readonly Lock @lock = new();
        private readonly Dictionary<uint, TrackedItem> _items = new();
        private readonly Timer timer;

        protected IAppSettingsService AppSettingsService { get; }

        public event EventHandler? StateChanged;

        public TimedEventTracker(IAppSettingsService appSettingsService, TimeSpan? tickInterval = null)
        {
            AppSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));

            var interval = tickInterval ?? TimeSpan.FromMilliseconds(100);
            timer = new Timer(OnTick, null, interval, interval);

            AppSettingsService.SettingsChanged += HandleSettingsChanged;
        }

        protected virtual bool ShouldTrack(TimedEvent evt) => true;

        protected virtual void OnSettingsChanged()
        {
        }

        private void HandleSettingsChanged(object? sender, EventArgs e) => OnSettingsChanged();

        public void Register(TimedEvent evt)
        {
            if (!ShouldTrack(evt)) return;

            lock (@lock)
            {
                if (_items.TryGetValue(evt.Id, out var existing))
                {
                    existing.Name = evt.Name;
                    existing.IconUrl = evt.IconUrl;
                    existing.Duration = evt.Duration;
                    existing.StartUtc = DateTime.UtcNow;
                }
                else
                {
                    _items[evt.Id] = new TrackedItem
                    {
                        Id = evt.Id,
                        Name = evt.Name,
                        IconUrl = evt.IconUrl,
                        Duration = evt.Duration,
                        StartUtc = DateTime.UtcNow
                    };
                }
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(uint id)
        {
            bool removed;
            lock (@lock) removed = _items.Remove(id);
            if (removed) StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            bool hadAny;
            lock (@lock)
            {
                hadAny = _items.Count > 0;
                _items.Clear();
            }
            if (hadAny) StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyList<TimedItemState> Items
        {
            get
            {
                var now = DateTime.UtcNow;
                lock (@lock)
                {
                    return _items.Values
                        .Select(i => ToState(i, now))
                        .Where(s => s.TimeLeft > TimeSpan.Zero)
                        .ToList();
                }
            }
        }

        private static TimedItemState ToState(TrackedItem item, DateTime nowUtc)
        {
            var elapsed = nowUtc - item.StartUtc;
            var timeLeft = item.Duration - elapsed;
            if (timeLeft < TimeSpan.Zero) timeLeft = TimeSpan.Zero;

            return new TimedItemState
            {
                Id = item.Id,
                Name = item.Name,
                IconUrl = item.IconUrl,
                Duration = item.Duration,
                TimeLeft = timeLeft
            };
        }

        private void OnTick(object? state)
        {
            bool expiredSomething;
            lock (@lock)
            {
                var now = DateTime.UtcNow;
                var expiredIds = _items.Values
                    .Where(i => (i.Duration - (now - i.StartUtc)) <= TimeSpan.Zero)
                    .Select(i => i.Id)
                    .ToList();

                foreach (var id in expiredIds) _items.Remove(id);
                expiredSomething = expiredIds.Count > 0;
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
            _ = expiredSomething; 
        }

        public void Dispose()
        {
            AppSettingsService.SettingsChanged -= HandleSettingsChanged;
            timer.Dispose();
        }
    }
}