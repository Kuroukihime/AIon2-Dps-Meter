using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessors.Shared;
using AionDpsMeter.Services.PacketProcessors.Shared.Exceptions;

namespace AionDpsMeter.Services.PacketProcessors.Damage
{
    internal class DamagePacketParser
    {
        private const int CriticalDamageType = 3;

        public static ParsedDamagePacket Parse(byte[] packet)
        {

            var reader = new PacketReader(packet);

            reader.ReadVarInt(); //len
            reader.ReadU16();    //opcode

            var targetId = reader.ReadVarInt();
            var switchValue = ProcessSwitchValue((int)reader.ReadVarInt());
            if(switchValue <  0 ) return Fail(PacketProcessResult.SWITH_VALUE_ERROR);
            reader.ReadVarInt(); // unkown flag
            var actorId = reader.ReadVarInt();

            if(actorId == targetId) return Fail(PacketProcessResult.ACTORID_EQUALS_TARGETID);

            var skillCode = reader.ReadU32();
            if (!DataValidationHelper.IsReasonableSkillCode((int)skillCode)) return Fail(PacketProcessResult.SKILLCODE_ERROR);
            reader.ReadU8(); // unknown
            var damageType = reader.ReadVarInt();
            var specialFlags = ReadSpecialBytesBlock(reader, switchValue);
            var unknownVarInt =reader.ReadVarInt(); // unknown
            var damage = reader.ReadVarInt();

            var data = new DamagePacketData
            {
                TargetId = (int)targetId,
                ActorId = (int)actorId,
                SkillCode = (int)skillCode,
                DamageType = (int)damageType,
                Damage = (long)damage,
                IsCritical = damageType == CriticalDamageType,
                IsBackAttack = specialFlags.IsBackAttack,
                IsFrontAttack = specialFlags.IsFrontAttack,
                IsParry = specialFlags.IsParry,
                IsPerfect = specialFlags.IsPerfect,
                IsDoubleDamage = specialFlags.IsDoubleDamage,
                UnknownVarInt = (int)unknownVarInt
            };

            return new ParsedDamagePacket(data, PacketProcessResult.SUCCES);

        }

        private static int ProcessSwitchValue(int switchVar)
        {
            if (switchVar > 255) return -1;
            int switchVal = switchVar & 0x0F;
            if (switchVal == 0 || (uint)(switchVal - 4) > 3) return -1;
            return switchVal;
        }

        private static SpecialFlags ReadSpecialBytesBlock(PacketReader reader, int switchValue)
        {
            SpecialFlags flags = default;
            byte attackDirectionType = 0;
            byte damageFlagByte = 0;
            if (switchValue == 4)
            {
                if (reader.Remaining < 8) throw new PacketProcessException("Not enough bytes remaining for special flags");
            }
            else 
            {
                if (reader.Remaining < 12) throw new PacketProcessException("Not enough bytes remaining for special flags");
                damageFlagByte = reader.ReadU8();
                reader.ReadU8(); // unknown
                attackDirectionType = reader.ReadU8();
            }

            flags = ParseSpecialFlags(damageFlagByte);
           
            flags.IsBackAttack = (attackDirectionType & 0x01) != 0;
            flags.IsFrontAttack = (attackDirectionType & 0x02) != 0;

            reader.ReadU32(); // unknown

            reader.Skip(4); //unknown tail bytes

            return flags;

        }

        private static ParsedDamagePacket Fail(PacketProcessResult result) => new(null, result);

        public struct SpecialFlags
        {
            public bool IsBackAttack { get; set; }
            public bool IsFrontAttack { get; set; }
            public bool IsParry { get; init; }
            public bool IsPerfect { get; init; }
            public bool IsDoubleDamage { get; init; }
        }

        private static SpecialFlags ParseSpecialFlags(byte flagByte) => new()
        {
            IsBackAttack = (flagByte & 0x01) != 0,
            IsParry = (flagByte & 0x02) != 0,
            IsPerfect = (flagByte & 0x04) != 0,
            IsDoubleDamage = (flagByte & 0x08) != 0
        };
    }
}
