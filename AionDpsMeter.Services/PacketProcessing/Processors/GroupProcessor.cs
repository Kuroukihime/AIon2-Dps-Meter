using System.Text;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    /// <summary>
    /// Tracks the user's party and force from the group roster messages.
    /// </summary>
    [PacketOpcode(PacketOpcodes.GroupRoster)]
    [PacketOpcode(PacketOpcodes.GroupLeft)]
    [PacketOpcode(PacketOpcodes.PartyMemberJoined)]
    [PacketOpcode(PacketOpcodes.ForceRoster)]
    [PacketOpcode(PacketOpcodes.ForceMemberJoined)]
    [PacketOpcode(PacketOpcodes.ForcePartyAdded)]
    [PacketOpcode(PacketOpcodes.ForceMemberLeft)]
    [PacketOpcode(PacketOpcodes.ForceLeft)]
    internal sealed class GroupProcessor(GroupTracker groupTracker, EntityTracker entityTracker, ILogger<GroupProcessor> logger) : IOpcodeProcessor
    {
        private const byte GuidTextLength = 36;
        private const int MaxNameBytes = 64;
        private const int MaxLevel = 60;
        private const int MaxItemLevel = 9_999;
        private const long MinCombatPower = 1_000;
        private const long MaxCombatPower = 10_000_000;

        public void Process(Packet packet)
        {
            var r = new PacketReader(packet.Data);
            r.ReadVarInt();
            ushort opcode = r.ReadU16();

            switch (opcode)
            {
                case PacketOpcodes.GroupRoster:
                    groupTracker.SetParty(ReadAndRecordMembers(packet.Data));
                    break;
                case PacketOpcodes.GroupLeft:
                    groupTracker.ClearParty();
                    break;
                case PacketOpcodes.PartyMemberJoined:
                    groupTracker.AddPartyMembers(ReadAndRecordMembers(packet.Data));
                    break;
                case PacketOpcodes.ForceRoster:
                    groupTracker.SetForce(ReadAndRecordMembers(packet.Data));
                    break;
                case PacketOpcodes.ForceMemberJoined:
                case PacketOpcodes.ForcePartyAdded:
                    groupTracker.AddForceMembers(ReadAndRecordMembers(packet.Data));
                    break;
                case PacketOpcodes.ForceMemberLeft:
                    groupTracker.RemoveForceMember((int)(r.ReadU64() & 0xFFFFFFFF));
                    break;
                case PacketOpcodes.ForceLeft:
                    groupTracker.ClearForce();
                    break;
            }

            var (party, force) = groupTracker.Counts;
            logger.LogDebug("Group 0x{Opcode:X4}: party={Party} force={Force}", opcode, party, force);
        }

        private List<GroupTracker.Member> ReadAndRecordMembers(byte[] data)
        {
            var members = ReadMembers(data);
            foreach (var m in members.Where(m => m.ItemLevel > 0 || m.CombatPower > 0))
                entityTracker.SetGroupStats(m.Id, m.Name, m.ItemLevel, m.CombatPower);
            return members;
        }

        /// <summary>
        /// Finds every member block by its anchor: [u16 server][u16][0x24][36-char GUID text][u64 dbid][u8 len][name].
        /// The rest of a block varies (stats, guild name), so the anchor is validated instead of decoding every field:
        /// the GUID must have its 8-4-4-4-12 shape and the dbid's top 16 bits must equal the server id before it.
        /// </summary>
        internal static List<GroupTracker.Member> ReadMembers(byte[] data)
        {
            var found = new List<(GroupTracker.Member Member, int AnchorIndex, int NameEnd)>();
            int p = 4;

            while (p + 1 + GuidTextLength + 8 + 1 <= data.Length)
            {
                if (data[p] == GuidTextLength && TryReadMember(data, p, out var member, out int next))
                {
                    found.Add((member, p, next));
                    p = next;
                    continue;
                }
                p++;
            }

            var members = new List<GroupTracker.Member>(found.Count);
            for (int i = 0; i < found.Count; i++)
            {
                int blockEnd = i + 1 < found.Count ? found[i + 1].AnchorIndex : data.Length;
                var (itemLevel, combatPower) = ReadMemberStats(data, found[i].NameEnd, blockEnd);
                members.Add(found[i].Member with { ItemLevel = itemLevel, CombatPower = combatPower });
            }

            return members;
        }

        /// <summary>
        /// After the name, a block holds [u32 level][u32 0][u32 item level], at a varying offset (guild name and varints
        /// come first), followed in party rosters by [u64 combat power]. Values outside plausible ranges are left at 0.
        /// </summary>
        private static (int ItemLevel, int CombatPower) ReadMemberStats(byte[] data, int start, int end)
        {
            for (int off = start; off + 12 <= end; off++)
            {
                uint level = BitConverter.ToUInt32(data, off);
                if (level is 0 or > MaxLevel || BitConverter.ToUInt32(data, off + 4) != 0) continue;

                uint itemLevel = BitConverter.ToUInt32(data, off + 8);
                if (itemLevel is 0 or > MaxItemLevel) continue;

                long combatPower = off + 20 <= end ? BitConverter.ToInt64(data, off + 12) : 0;
                bool combatPowerValid = combatPower is >= MinCombatPower and <= MaxCombatPower;
                return ((int)itemLevel, combatPowerValid ? (int)combatPower : 0);
            }

            return (0, 0);
        }

        private static bool TryReadMember(byte[] data, int lengthIndex, out GroupTracker.Member member, out int next)
        {
            member = default;
            next = 0;

            int guidStart = lengthIndex + 1;
            if (!IsGuidText(data.AsSpan(guidStart, GuidTextLength))) return false;

            int dbidIndex = guidStart + GuidTextLength;
            ulong dbid = BitConverter.ToUInt64(data, dbidIndex);
            ushort server = BitConverter.ToUInt16(data, lengthIndex - 4);
            if ((ushort)(dbid >> 48) != server) return false;

            int nameLength = data[dbidIndex + 8];
            int nameStart = dbidIndex + 9;
            if (nameLength is 0 or > MaxNameBytes || nameStart + nameLength > data.Length) return false;

            string name;
            try
            {
                name = new UTF8Encoding(false, true).GetString(data, nameStart, nameLength);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            member = new GroupTracker.Member((int)(dbid & 0xFFFFFFFF), name);
            next = nameStart + nameLength;
            return true;
        }

        private static bool IsGuidText(ReadOnlySpan<byte> text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                bool dash = i is 8 or 13 or 18 or 23;
                if (dash ? text[i] != '-' : !Uri.IsHexDigit((char)text[i])) return false;
            }
            return true;
        }
    }
}
