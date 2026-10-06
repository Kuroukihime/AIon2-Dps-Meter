using AionDpsMeter.Services.Models;
using System.Globalization;

namespace AionDpsMeter.UI.Utils
{
    public static class CopyDpsFormatter
    {
        private const int MaxPlayers = 10;

        public static string Format(IEnumerable<PlayerStats> players)
        {
            ArgumentNullException.ThrowIfNull(players);

            return string.Join(", ", players
                .OrderByDescending(player => player.TotalDamage)
                .Take(MaxPlayers)
                .Select(player =>
                {
                    var dps = ((long)player.DamagePerSecond).ToString("N0", CultureInfo.InvariantCulture);
                    var totalDamage = player.TotalDamage.ToString("N0", CultureInfo.InvariantCulture);
                    return $"{player.PlayerName} : {dps} DPS ({totalDamage} dmg)";
                }));
        }
    }
}
