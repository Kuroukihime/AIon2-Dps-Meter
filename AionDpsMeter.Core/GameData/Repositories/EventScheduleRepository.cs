using System.Globalization;
using System.Text.Json;
using AionDpsMeter.Core.GameData.Contracts;

namespace AionDpsMeter.Core.GameData.Repositories
{
    /// <summary>
    /// A daily recurring schedule: wall-clock spawn times in a named time zone (DST-aware).
    /// </summary>
    public sealed class EventScheduleRepository
    {
        private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;
        private readonly List<TimeSpan> _spawnTimes = [];

        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Event schedule data file not found: {path}");

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var file = JsonSerializer.Deserialize<EventScheduleFile>(json, options)
                ?? throw new InvalidDataException($"Failed to deserialize {Path.GetFileName(path)}");

            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(file.TimeZone);
            _spawnTimes.Clear();
            foreach (var time in file.SpawnTimes)
                _spawnTimes.Add(TimeSpan.ParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Earliest spawn strictly after <paramref name="utcNow"/>, or null when no times are loaded.
        /// </summary>
        public DateTime? NextOccurrenceUtc(DateTime utcNow)
        {
            var today = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _timeZone).Date;
            DateTime? next = null;

            for (var dayOffset = 0; dayOffset <= 1; dayOffset++)
            {
                var day = today.AddDays(dayOffset);
                foreach (var time in _spawnTimes)
                {
                    var wallClock = DateTime.SpecifyKind(day + time, DateTimeKind.Unspecified);
                    if (_timeZone.IsInvalidTime(wallClock))
                        continue;

                    var candidate = TimeZoneInfo.ConvertTimeToUtc(wallClock, _timeZone);
                    if (candidate > utcNow && (next is null || candidate < next))
                        next = candidate;
                }
            }

            return next;
        }
    }
}
