using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{

    public static class BuffStatisticsCalculator
    {
        public static IReadOnlyCollection<BuffStats> ComputeBuffStats(IReadOnlyList<BuffEvent> buffEvents)
        {
            if (buffEvents.Count == 0)
                return [];

            return buffEvents
                .GroupBy(e => e.BuffId)
                .Select(g =>
                {
                    var intervals = g
                        .Select(e => (Start: e.AppliedAt, End: e.AppliedAt.AddMilliseconds(e.DurationMs)))
                        .OrderBy(i => i.Start)
                        .ToList();

                    double mergedSeconds = MergeIntervals(intervals);

                    var first = g.First();
                    return new BuffStats
                    {
                        BuffId = g.Key,
                        BuffName = first.BuffName,
                        BuffIcon = first.BuffIcon,
                        Description = first.Description,
                        ApplicationCount = g.Count(),
                        EffectiveDurationSeconds = mergedSeconds,
                    };
                })
                .OrderByDescending(b => b.ApplicationCount)
                .ToList();
        }


        private static double MergeIntervals(List<(DateTime Start, DateTime End)> sorted)
        {
            if (sorted.Count == 0) return 0;

            double totalSeconds = 0;
            var currentStart = sorted[0].Start;
            var currentEnd = sorted[0].End;

            for (int i = 1; i < sorted.Count; i++)
            {
                var (start, end) = sorted[i];
                if (start <= currentEnd)
                {
                   
                    if (end > currentEnd)
                        currentEnd = end;
                }
                else
                {
                    totalSeconds += (currentEnd - currentStart).TotalSeconds;
                    currentStart = start;
                    currentEnd = end;
                }
            }

            totalSeconds += (currentEnd - currentStart).TotalSeconds;
            return totalSeconds;
        }
    }
}
