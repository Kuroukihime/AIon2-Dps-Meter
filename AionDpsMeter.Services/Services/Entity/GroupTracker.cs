using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.Services.Entity
{
    /// <summary>
    /// The user's current party and force, keyed by character (global) id. Written by the packet thread, read by the meter.
    /// </summary>
    public sealed class GroupTracker
    {
        public readonly record struct Member(int Id, string Name, int ItemLevel = 0, int CombatPower = 0);

        private readonly Lock _lock = new();
        private readonly Dictionary<int, string> _party = new();
        private readonly Dictionary<int, string> _force = new();

        public bool IsGrouped
        {
            get { lock (_lock) return _party.Count > 0 || _force.Count > 0; }
        }

        public (int Party, int Force) Counts
        {
            get { lock (_lock) return (_party.Count, _force.Count); }
        }

        public void SetParty(IEnumerable<Member> members)
        {
            lock (_lock) Replace(_party, members);
        }

        public void AddPartyMembers(IEnumerable<Member> members)
        {
            lock (_lock)
            {
                foreach (var m in members) _party[m.Id] = m.Name;
            }
        }

        public void ClearParty()
        {
            lock (_lock) _party.Clear();
        }

        public void SetForce(IEnumerable<Member> members)
        {
            lock (_lock) Replace(_force, members);
        }

        public void AddForceMembers(IEnumerable<Member> members)
        {
            lock (_lock)
            {
                foreach (var m in members) _force[m.Id] = m.Name;
            }
        }

        public void RemoveForceMember(int id)
        {
            lock (_lock) _force.Remove(id);
        }

        public void ClearForce()
        {
            lock (_lock) _force.Clear();
        }

        // Party wins over force: the user's own party inside a force is also listed in the force roster.
        public GroupKind GetGroupKind(Player? player)
        {
            if (player is null) return GroupKind.None;

            lock (_lock)
            {
                if (Contains(_party, player)) return GroupKind.Party;
                if (Contains(_force, player)) return GroupKind.Force;
                return GroupKind.None;
            }
        }

        // Rosters carry global ids; a session player whose id link hasn't arrived yet is matched by its confirmed name.
        private static bool Contains(Dictionary<int, string> members, Player player) =>
            player.GlobalId is { } id
                ? members.ContainsKey(id)
                : player.IsIdentified && members.ContainsValue(player.Name);

        private static void Replace(Dictionary<int, string> target, IEnumerable<Member> members)
        {
            target.Clear();
            foreach (var m in members) target[m.Id] = m.Name;
        }
    }
}
