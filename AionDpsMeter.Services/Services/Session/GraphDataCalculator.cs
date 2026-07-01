using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    public static class GraphDataCalculator
    {
        private const int SmoothRadiusSeconds = 2; // ±2 sec → 5-second rolling window

        public static (IReadOnlyList<DpsDataPoint> Points, IReadOnlyList<BuffTimelineEntry> BuffTimeline, double TotalSec)
            Compute(IReadOnlyList<PlayerDamage> hitsNewestFirst, IReadOnlyList<BuffEvent> buffEvents)
        {
            if (hitsNewestFirst.Count == 0)
                return ([], [], 0);

            DateTime firstHit = hitsNewestFirst[^1].DateTime;
            DateTime lastHit = hitsNewestFirst[0].DateTime;
            double totalSec = Math.Max((lastHit - firstHit).TotalSeconds, 0.1);

            int buckets = (int)Math.Ceiling(totalSec);

            long[] bucketDamage = BuildDamageBuckets(hitsNewestFirst, firstHit, buckets);
            long[] smoothed = SmoothDamage(bucketDamage, buckets);
            var points = BuildDataPoints(bucketDamage, smoothed, buckets, totalSec);

            var timeline = BuildBuffTimeline(buffEvents, firstHit, totalSec);

            return (points, timeline, totalSec);
        }

        private static long[] BuildDamageBuckets(IReadOnlyList<PlayerDamage> hitsNewestFirst, DateTime firstHit, int buckets)
        {
            var bucketDamage = new long[buckets + 1];

            foreach (var hit in hitsNewestFirst)
            {
                double offset = (hit.DateTime - firstHit).TotalSeconds;
                int idx = Math.Max(0, Math.Min((int)offset, buckets));
                bucketDamage[idx] += hit.Damage;
            }

            return bucketDamage;
        }

        // Apply a rolling average window to smooth per-second damage spikes
        // caused by network packet batching.
        private static long[] SmoothDamage(long[] bucketDamage, int buckets)
        {
            var smoothed = new long[buckets + 1];

            for (int i = 0; i <= buckets; i++)
            {
                int lo = Math.Max(0, i - SmoothRadiusSeconds);
                int hi = Math.Min(buckets, i + SmoothRadiusSeconds);

                long sum = 0;
                for (int j = lo; j <= hi; j++)
                    sum += bucketDamage[j];

                smoothed[i] = sum / (hi - lo + 1);
            }

            return smoothed;
        }

        private static List<DpsDataPoint> BuildDataPoints(long[] bucketDamage, long[] smoothed, int buckets, double totalSec)
        {
            var points = new List<DpsDataPoint>(buckets + 1);
            long cumDmg = 0;

            for (int i = 0; i <= buckets; i++)
            {
                cumDmg += bucketDamage[i];
                double elapsed = Math.Max(0.1, Math.Min(i + 1.0, totalSec));
                points.Add(new DpsDataPoint(i, smoothed[i], cumDmg / elapsed));
            }

            return points;
        }

        /// <summary>
        /// Builds the buff timeline for the graph, merging overlapping (or touching)
        /// applications of the same buff into single continuous bars so the graph
        /// doesn't double-count or visually stack identical buffs applied at the same time.
        /// </summary>
        private static List<BuffTimelineEntry> BuildBuffTimeline(IReadOnlyList<BuffEvent> buffEvents, DateTime firstHit, double totalSec)
        {
            var timeline = new List<BuffTimelineEntry>();

            foreach (var group in buffEvents.GroupBy(e => e.BuffId))
            {
                var intervals = GetClampedIntervals(group, firstHit, totalSec);
                if (intervals.Count == 0)
                    continue;

                var first = group.First();
                timeline.AddRange(MergeOverlappingIntervals(intervals, first));
            }

            timeline.Sort((a, b) => a.StartSec.CompareTo(b.StartSec));
            return timeline;
        }

        private static List<(double Start, double End)> GetClampedIntervals(
            IEnumerable<BuffEvent> events, DateTime firstHit, double totalSec)
        {
            return events
                .Select(e =>
                {
                    double start = Math.Max(0, (e.AppliedAt - firstHit).TotalSeconds);
                    double end = Math.Min(totalSec, (e.AppliedAt.AddMilliseconds(e.DurationMs) - firstHit).TotalSeconds);
                    return (Start: start, End: end);
                })
                .Where(i => i.End > i.Start)
                .OrderBy(i => i.Start)
                .ToList();
        }

        private static List<BuffTimelineEntry> MergeOverlappingIntervals(
            List<(double Start, double End)> sortedIntervals, BuffEvent buffInfo)
        {
            var merged = new List<BuffTimelineEntry>();

            double currentStart = sortedIntervals[0].Start;
            double currentEnd = sortedIntervals[0].End;
            int currentCount = 1;

            for (int i = 1; i < sortedIntervals.Count; i++)
            {
                var (start, end) = sortedIntervals[i];

                if (start <= currentEnd)
                {
                    // Overlapping or touching — extend the current run,
                    // but still count this as a separate application
                    if (end > currentEnd)
                        currentEnd = end;
                    currentCount++;
                }
                else
                {
                    // Gap — flush current run, start a new one
                    merged.Add(new BuffTimelineEntry(buffInfo.BuffId, buffInfo.BuffName, buffInfo.BuffIcon, currentStart, currentEnd, currentCount));
                    currentStart = start;
                    currentEnd = end;
                    currentCount = 1;
                }
            }

            merged.Add(new BuffTimelineEntry(buffInfo.BuffId, buffInfo.BuffName, buffInfo.BuffIcon, currentStart, currentEnd, currentCount));
            return merged;
        }
    }
}