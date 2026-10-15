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
                    return $"{player.PlayerName} : {DamageFormatter.Format(player.DamagePerSecond)} DPS ({DamageFormatter.Format(player.TotalDamage)} dmg)";
                }));
        }
    }
}
