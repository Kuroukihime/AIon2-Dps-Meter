using System.Diagnostics;
using AionDpsMeter.Services.PacketProcessors.PlayerEntity.GamePackets;
using AionDpsMeter.Services.PacketProcessors.Shared;

namespace AionDpsMeter.Services.PacketProcessors.PlayerEntity
{

    //CAN NC STOP CHANGING THIS PACKET ALREADY FFS
    public static class PartyPacketParser
    {
        private const byte MaskHasUnknown01 = 0x01;
        private const byte MaskHasUnknown02 = 0x02;
        private const byte MaskHasGearScore = 0x04;
        private const byte MaskHasUnknown08 = 0x08;
        private const byte MaskHasCombatPower = 0x10;
        private const byte MaskHasUnknown20 = 0x20;
        private const byte MaskHasUnknown40 = 0x40;


        public static PartyPacket Parse(byte[] packet, int offset = 0)
        {


            Debug.WriteLine(BitConverter.ToString(packet));
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
                r.ReadU32();                 // unnamed

            uint? gearScore = null;
            if ((mask & MaskHasGearScore) != 0)
                gearScore = r.ReadU32();     // _equip_item_level

            if ((mask & MaskHasUnknown08) != 0)
                r.ReadBit();                  // unnamed (bit)

            r.ReadBit();                 // unnamed (bit), always present
            r.ReadU16();                 // unnamed, always present
            r.ReadU16();                 // unnamed, always present
            r.ReadU8();                  // unnamed, always present

            ulong? combatPower = null;
            if ((mask & MaskHasCombatPower) != 0)
                combatPower = r.ReadU64();   // _combat_power

            var trailingArrayCount = r.ReadVarInt();

            if (trailingArrayCount > 0)
            {
                for (int j = 0; j < trailingArrayCount; j++)
                {
                    r.ReadU8();
                    r.ReadU32();

                }
            }

            if ((mask & MaskHasUnknown20) != 0)
            {
                r.ReadU64();                 // unnamed      
            }
            r.ReadU8();         // unnamed, always present
            r.ReadU8();         // unnamed, always present

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