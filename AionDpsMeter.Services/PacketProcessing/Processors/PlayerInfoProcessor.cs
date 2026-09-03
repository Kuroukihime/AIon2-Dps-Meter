using AionDpsMeter.Core.Data;
using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.PlayerInfo)]
    internal class PlayerInfoProcessor(EntityTracker entityTracker) : BasePlayerInfoProcessor , IOpcodeProcessor
    {
        public void Process(Packet packet)
        {
            if (!TryParseInfo(packet.Data, out PlayerInfoResult result)) return;
            entityTracker.SetSessionPlayerName(result.EntityId, result.Name, result.CombatPower, ServerMap.GetName(result.ServerId), true);
        }

        private bool TryParseInfo(byte[] data, out PlayerInfoResult result)
        {
            result = default;
            var endOffset = data.Length;
            var pos = data.ReadVarInt().Length + 2;
            if (pos >= endOffset) return false;

            var entityVarInt = data.ReadVarInt(pos);
            int entityId = entityVarInt.Value;
            if (entityId < 1) return false;
            pos += entityVarInt.Length;

            var nameRead = ReadPlayerName(data, pos, endOffset);
            if (!nameRead.HasValue) return false;

            int afterNameOffset = nameRead.Value.EndOffset;
            int serverId = afterNameOffset + 2 <= endOffset ? data[afterNameOffset] | (data[afterNameOffset + 1] << 8) : -1;
            int jobCode = afterNameOffset + 3 <= endOffset ? data[afterNameOffset + 2] : -1;

            int combatPower = TryParseCombatPower(data, endOffset, out int parsedCombatPower) ? parsedCombatPower : 0;
            result = new PlayerInfoResult(entityId, nameRead.Value.Name, serverId, jobCode, combatPower);
            return true;
        }
    }
}
