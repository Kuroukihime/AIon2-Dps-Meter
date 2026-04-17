using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    public static class GraphDataCalculator
    {
        public static (IReadOnlyList<DpsDataPoint> Points, IReadOnlyList<BuffTimelineEntry> BuffTimeline, double TotalSec)
            Compute(IReadOnlyList<PlayerDamage> hitsNewestFirst, IReadOnlyList<BuffEvent> buffEvents)
        {
            if (hitsNewestFirst.Count == 0)
                return ([], [], 0);

            DateTime firstHit = hitsNewestFirst[^1].DateTime;
            DateTime lastHit  = hitsNewestFirst[0].DateTime;
            double totalSec   = Math.Max((lastHit - firstHit).TotalSeconds, 0.1);

            int buckets = (int)Math.Ceiling(totalSec);
            var bucketDamage = new long[buckets + 1];

            foreach (var hit in hitsNewestFirst)
            {
                double offset = (hit.DateTime - firstHit).TotalSeconds;
                int idx = Math.Max(0, Math.Min((int)offset, buckets));
                bucketDamage[idx] += hit.Damage;
            }

            var points = new List<DpsDataPoint>(buckets + 1);
            long cumDmg = 0;
            for (int i = 0; i <= buckets; i++)
            {
                cumDmg += bucketDamage[i];
                double elapsed = Math.Max(0.1, Math.Min(i + 1.0, totalSec));
                points.Add(new DpsDataPoint(i, bucketDamage[i], cumDmg / elapsed));
            }

            var timeline = new List<BuffTimelineEntry>(buffEvents.Count);
            foreach (var e in buffEvents)
            {
                double start = Math.Max(0, (e.AppliedAt - firstHit).TotalSeconds);
                double end   = Math.Min(totalSec, (e.AppliedAt.AddMilliseconds(e.DurationMs) - firstHit).TotalSeconds);
                if (end > start)
                    timeline.Add(new BuffTimelineEntry(e.BuffId, e.BuffName, e.BuffIcon, start, end));
            }

            return (points, timeline, totalSec);
        }
    }
}
