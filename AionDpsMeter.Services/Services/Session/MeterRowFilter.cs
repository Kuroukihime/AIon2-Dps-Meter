using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Session
{
    /// <summary>Which rows the meter lists: only the user in solo, only the group while grouped, otherwise everyone.</summary>
    public static class MeterRowFilter
    {
        // Solo never lists other players; until the user's row is recognized it lists nothing.
        public static List<PlayerStats> Apply(IEnumerable<PlayerStats> stats, bool isGrouped, bool isSoloActive, out bool waitingForUser)
        {
            var all = stats.ToList();

            if (isSoloActive)
            {
                var mine = all.Where(s => s.IsUser).ToList();
                waitingForUser = mine.Count == 0;
                return mine;
            }

            waitingForUser = false;
            return isGrouped ? all.Where(s => s.IsUser || s.Group != GroupKind.None).ToList() : all;
        }
    }
}
