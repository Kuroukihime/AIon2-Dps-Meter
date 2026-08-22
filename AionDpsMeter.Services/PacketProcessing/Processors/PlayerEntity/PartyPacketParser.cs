using System.Diagnostics;
using AionDpsMeter.Services.PacketProcessing.Processors.PlayerEntity.GamePackets;
using AionDpsMeter.Services.PacketProcessing.Shared;

namespace AionDpsMeter.Services.PacketProcessing.Processors.PlayerEntity
{

    //CAN NC STOP CHANGING THIS PACKET ALREADY FFS
    public static class PartyPacketParser
    {
        private const byte MaskHasUnknown01 = 0x01;
        private const byte MaskHasUnknown02 = 0x02;
        private const byte MaskHasUnknown04 = 0x04;
        private const byte MaskHasUnknown08 = 0x08;
        private const byte MaskHasUnknown10 = 0x10;
        private const byte MaskHasUnknown20 = 0x20;
        private const byte MaskHasUnknown40 = 0x40;


        public static PartyPacket Parse(byte[] packet, int offset = 0)
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

            uint gearScore  = r.ReadU32();     // _equip_item_level,  always present


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
    }
}