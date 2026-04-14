using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessors.Shared;

namespace AionDpsMeter.Services.PacketProcessors.Damage
{
    internal ref struct DamagePacketReader
    {
        private const byte Opcode1 = 0x04;
        private const byte Opcode2 = 0x38;

        private PacketReader inner;

        public byte DamageFlagByte;
        public byte ControlByte;

        public DamagePacketReader(byte[] data)
        {
            inner = new PacketReader(data);
        }

        public bool ReadAndValidateHeader() =>
            inner.ReadAndValidateHeader(Opcode1, Opcode2);

        public bool ReadTargetId(out int targetId) =>
            inner.ReadVarIntId(out targetId);

        public bool ReadAndValidateSwitchValue(out int switchValue)
        {
            switchValue = 0;
            var (switchVar, switchBytes) = inner.Data.ReadVarInt(inner.Offset);
            if (switchBytes <= 0 || switchBytes > 1) return false;

            int switchVal = switchVar & 0x0F;
            if (switchVal == 0 || (uint)(switchVal - 4) > 3) return false;

            switchValue = switchVal;
            // advance via ReadVarInt discard
            inner.ReadVarInt(out _);
            return inner.HasRemainingBytes();
        }

        public bool SkipFlagField()
        {
            var (_, flagBytes) = inner.Data.ReadVarInt(inner.Offset);
            if (flagBytes <= 0 || flagBytes > 1) return false;
            inner.ReadVarInt(out _);
            return inner.HasRemainingBytes();
        }

        public bool ReadActorId(out int actorId) =>
            inner.ReadVarIntId(out actorId);

        public bool ReadSkillCode(out int skillCode)
        {
            skillCode = 0;
            if (!inner.HasRemainingBytes(5)) return false;

            skillCode = inner.Data.ReadUInt32Le(inner.Offset);
            // manually advance 5 bytes (4 skill code + 1 unknown)
            for (int i = 0; i < 5; i++) inner.ReadByte(out _);

            return DataValidationHelper.IsReasonableSkillCode(skillCode) && inner.HasRemainingBytes();
        }

        public bool ReadDamageType(out int damageType)
        {
            damageType = 0;
            var (type, bytes) = inner.Data.ReadVarInt(inner.Offset);
            if (bytes <= 0) return false;
            damageType = type;
            inner.ReadVarInt(out _);
            return inner.HasRemainingBytes();
        }

        public bool ReadSpecialFlags(int switchValue, out SpecialFlags flags)
        {
            flags = default;

            if (switchValue == 4)
            {
                if (!inner.HasRemainingBytes(8)) return false;
                DamageFlagByte = 0;
            }
            else
            {
                if (!inner.HasRemainingBytes(10)) return false;
                inner.ReadByte(out DamageFlagByte);
                inner.ReadByte(out _); // unknown byte
            }

            flags = ParseSpecialFlags(DamageFlagByte);

            // skip 4-byte unknown uint32
            for (int i = 0; i < 4; i++) inner.ReadByte(out _);

            inner.ReadByte(out ControlByte);
            int tailLen = ControlByte > 8 ? 2 : 3;
            for (int i = 0; i < tailLen; i++) inner.ReadByte(out _);

            return inner.HasRemainingBytes();
        }

        public bool ReadUnknownVarInt(out int value) =>
            inner.ReadVarInt(out value);

        public bool ReadDamage(out long damage)
        {
            damage = 0;

            int dmgOffset = inner.Offset;
            var (dmg, bytes) = inner.Data.ReadVarInt(dmgOffset);
            if (bytes <= 0) return false;

            inner.ReadVarInt(out _);
            damage = dmg;
            return true;
        }

        public readonly struct SpecialFlags
        {
            public bool IsBackAttack { get; init; }
            public bool IsParry { get; init; }
            public bool IsPerfect { get; init; }
            public bool IsDoubleDamage { get; init; }
        }

        private static SpecialFlags ParseSpecialFlags(byte flagByte) => new()
        {
            IsBackAttack  = (flagByte & 0x01) != 0,
            IsParry       = (flagByte & 0x04) != 0,
            IsPerfect     = (flagByte & 0x08) != 0,
            IsDoubleDamage = (flagByte & 0x10) != 0
        };
    }
}
