using System.Diagnostics;
using AionDpsMeter.Services.PacketProcessors.PlayerEntity.GamePackets;
using AionDpsMeter.Services.PacketProcessors.Shared;

namespace AionDpsMeter.Services.PacketProcessors.PlayerEntity
{
    public static class PartyPacketParser
    {
        // presence_mask bits
        private const byte Mask_HasUnknown01 = 0x01;
        private const byte Mask_HasGearScore = 0x02;
        private const byte Mask_HasUnknown04 = 0x04;
        private const byte Mask_HasUnknown08 = 0x08;
        private const byte Mask_HasUnknown10 = 0x10;
        private const byte Mask_HasCombatPower = 0x20;
        private const byte Mask_HasUnknown40 = 0x40;

        public static PartyPacket Parse(byte[] packet, int offset = 0)
        {

            var r = new PacketReader2(packet);

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

        private static PartyPlayerPacket ParseMember(PacketReader2 r)
        {

            var position = r.Position;

            byte mask = r.ReadU8();          // presence_mask
            byte slot = r.ReadU8();          // _number
            ulong dbid = r.ReadU64();        // _dbid
            string nickname = r.ReadLengthPrefixedString(); // _nickname

            r.ReadU32();                     // unnamed, always present
            uint level = r.ReadU32();        // _level, always present

            if ((mask & Mask_HasUnknown01) != 0)
                r.ReadU32();                 // unnamed [mask&0x02]

            uint? gearScore = null;
            if ((mask & Mask_HasGearScore) != 0)
                gearScore = r.ReadU32();     // _equip_item_level [mask&0x04]

             r.ReadBit();                      // unnamed (bit), always present

            if ((mask & Mask_HasUnknown04) != 0)
                r.ReadBit();                  // unnamed (bit) [mask&0x04]


            if ((mask & Mask_HasUnknown08) != 0)
                r.ReadU16();                  // unnamed (bit) [mask&0x08]

            if ((mask & Mask_HasUnknown10) != 0)
                r.ReadU16();                 // unnamed [mask&0x10]

            r.ReadU8();                      // unnamed, always present

            ulong? combatPower = null;
            if ((mask & Mask_HasCombatPower) != 0)
                combatPower = r.ReadU64();   // _combat_power [mask&0x20]

            var trailingArrayCount = r.ReadVarInt();

            if( trailingArrayCount > 0)
            {
                for (int j = 0; j < trailingArrayCount; j++)
                {
                    r.ReadU32();
                }
            }

            if ((mask & Mask_HasUnknown40) != 0)
            {       
                r.ReadU64();                 // unnamed      
            }
            var nextMask = r.ReadU8();       // unnamed

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