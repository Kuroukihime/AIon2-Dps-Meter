using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessors.Shared;

namespace AionDpsMeter.Services.PacketProcessors.DotDamage
{
    internal ref struct DotDamagePacketReader
    {
        private const byte Opcode1 = 0x05;
        private const byte Opcode2 = 0x38;

        private PacketReader inner;

        public DotDamagePacketReader(byte[] data)
        {
            inner = new PacketReader(data);
        }

        public bool ReadAndValidateHeader() =>
            inner.ReadAndValidateHeader(Opcode1, Opcode2);

        public bool ReadTargetId(out int targetId) =>
            inner.ReadVarIntId(out targetId);

        public bool ReadAndValidateEffectType(out int effectType)
        {
            effectType = 0;
            if (!inner.ReadByte(out byte b)) return false;
            if ((b & 0x02) == 0) return false;
            effectType = b;
            return inner.HasRemainingBytes();
        }

        public bool ReadActorId(out int actorId) =>
            inner.ReadVarIntId(out actorId);

        public bool ReadUnknownVarInt(out int value) =>
            inner.ReadVarInt(out value);

        public bool ReadSkillCode(out int skillCode)
        {
            skillCode = 0;
            if (!inner.HasRemainingBytes(4)) return false;

            skillCode = inner.Data.ReadUInt32Le(inner.Offset) / 100;
            for (int i = 0; i < 4; i++) inner.ReadByte(out _);

            return DataValidationHelper.IsReasonableSkillCode(skillCode) && inner.HasRemainingBytes();
        }

        public bool ReadDamage(out long damage)
        {
            damage = 0;
            var (dmg, bytes) = inner.Data.ReadVarInt(inner.Offset);
            if (bytes <= 0 || dmg < 1) return false;
            inner.ReadVarInt(out _);
            damage = dmg;
            return true;
        }
    }
}
