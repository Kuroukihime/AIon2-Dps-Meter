using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Entity;
using System.Diagnostics;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.PartyInfo)]
    internal class PartyProcessor(EntityTracker entityTracker) : IOpcodeProcessor
    {
        private const byte MaskHasUnknown01 = 0x01;
        private const byte MaskHasUnknown02 = 0x02;
        private const byte MaskHasUnknown04 = 0x04;
        private const byte MaskHasUnknown08 = 0x08;
        private const byte MaskHasUnknown10 = 0x10;
        private const byte MaskHasUnknown20 = 0x20;
        private const byte MaskHasUnknown40 = 0x40;

        public void Process(Packet packet)
        {
            List<Player> list = ParsePartyMemberBlocksStructured(packet.Data);
            if (list.Count > 0)
            {
                foreach (var partyMember in list)
                    entityTracker.RegisterOrUpdateGlobalPlayer(partyMember);
            }
        }

        private List<Player> ParsePartyMemberBlocksStructured(byte[] packet)
        {

            var results = new List<Player>();

            try
            {

                var result = Parse(packet);

                foreach (var partyMember in result.ValidMembers)
                {
                    results.Add(new Player
                    {
                        Id = (int)partyMember.Id,
                        ServerId = partyMember.ServerId,
                        ServerName = partyMember.ServerName,
                        Name = partyMember.Name,
                        CharacterLevel = (int)partyMember.CharactedLevel,
                        CombatPower = (int)(partyMember.CombatPower ?? 0)
                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(BitConverter.ToString(packet));
            }

            return results;
        }

        private PartyPacket Parse(byte[] packet, int offset = 0)
        {

            var r = new PacketReader(packet);

            r.ReadVarInt(); //len
            r.ReadU16();  // opcode

            var party = new PartyPacket
            {
                PartyKey = r.ReadU32(),
                PartyName = r.ReadLengthPrefixedString(),
                PartySize = r.ReadU8(),
                DungeonId = r.ReadU32(),
            };

            r.ReadU8();                    // unnamed
            r.ReadU8();                    // unnamed
            party.LeaderDbid = r.ReadU64(); // _leader_dbid
            r.ReadBit();                    // unnamed (bit)
            r.ReadU8();                    // unnamed
            r.ReadU8();                    // unnamed

            uint memberCount = r.ReadVarInt();

            for (int i = 0; i < memberCount; i++)
                party.Members.Add(ParseMember(r));

            return party;
        }

        private static PartyPlayerPacket ParseMember(PacketReader r)
        {
            byte mask = r.ReadU8();          // presence_mask
            byte slot = r.ReadU8();          // _number
            ulong dbid = r.ReadU64();        // _dbid
            string nickname = r.ReadLengthPrefixedString(); // _nickname

            r.ReadU32();                     // unnamed, always present
            uint level = r.ReadU32();        // _level, always present

            if ((mask & MaskHasUnknown01) != 0)
                r.ReadU32();                   // _conqueror_level

            uint gearScore = r.ReadU32();     // _equip_item_level,  always present


            if ((mask & MaskHasUnknown02) != 0)
                r.ReadBit();                  // ready (bit)

            r.ReadBit();                 // login (bit), always present

            if ((mask & MaskHasUnknown04) != 0)
                r.ReadU16();                 // _born_server_id , always present

            if ((mask & MaskHasUnknown08) != 0)
                r.ReadU16();                 // _current_server_id, always present

            r.ReadU8();                  // _party_role, always present

            ulong combatPower = r.ReadU64();  // _combat_power, always present


            var trailingArrayCount = r.ReadVarInt(); // _contents_tickets

            if (trailingArrayCount > 0)
            {
                for (int j = 0; j < trailingArrayCount; j++)
                {
                    r.ReadU8();
                    r.ReadU32();

                }
            }

            if ((mask & MaskHasUnknown10) != 0)
            {
                r.ReadU64();    //_rebirth_myself_item_count       
            }
            r.ReadU8();         // _mentoring_role , always present
            r.ReadU8();         // _network_latency_state , always present

            return new PartyPlayerPacket
            {
                SlotNumber = slot,
                Dbid = dbid,
                Name = nickname,
                CharactedLevel = level,
                GearScore = gearScore,
                CombatPower = combatPower,
                PresenceMask = mask,
            };
        }


        private class PartyPacket
        {
            public uint PartyKey { get; set; }
            public string PartyName { get; set; } = string.Empty;
            public byte PartySize { get; set; }
            public uint DungeonId { get; set; }
            public ulong LeaderDbid { get; set; }
            public uint LeaderId => (uint)(LeaderDbid & 0xFFFFFFFF);
            public int LeaderServerId => (int)((LeaderDbid >> 48) & 0xFFFF);

            public List<PartyPlayerPacket> Members { get; } = new List<PartyPlayerPacket>();

            public IEnumerable<PartyPlayerPacket> ValidMembers
            {
                get
                {
                    foreach (var m in Members)
                        if (m.IsValid) yield return m;
                }
            }
        }

        /// <summary>One party member slot.</summary>
        private  class PartyPlayerPacket
        {
            /// <summary>1-based party slot number (_number).</summary>
            public byte SlotNumber { get; set; }

            /// <summary>Raw 64-bit GlobalID</summary>
            public ulong Dbid { get; set; }

            /// <summary>Low 32 bits of Dbid — the character id.</summary>
            public uint Id => (uint)(Dbid & 0xFFFFFFFF);

            /// <summary>Top 16 bits of Dbid — the server id.</summary>
            public int ServerId => (int)((Dbid >> 48) & 0xFFFF);

            public string ServerName => ServerMap.GetName(ServerId);

            public string Name { get; set; } = string.Empty;

            /// <summary>_level, always present.</summary>
            public uint CharactedLevel { get; set; }

            /// <summary>_equip_item_level, only present when presence_mask &amp; 0x04.</summary>
            public uint? GearScore { get; set; }

            /// <summary>_combat_power, only present when presence_mask &amp; 0x20.</summary>
            public ulong? CombatPower { get; set; }

            /// <summary>Raw presence_mask byte, kept for debugging/future field decoding.</summary>
            public byte PresenceMask { get; set; }

            /// <summary>Any player slot that is not empty.</summary>
            public bool IsValid => PresenceMask != 0;
        }
    }
}
