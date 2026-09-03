using AionDpsMeter.Core.Data;
using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.OtherPlayersInfo)]
    internal class OtherPlayerInfoProcessor(EntityTracker entityTracker) : BasePlayerInfoProcessor, IOpcodeProcessor
    {
        public void Process(Packet packet)
        {
            if (!TryParseInfo(packet.Data, out PlayerInfoResult result)) return;
            entityTracker.SetSessionPlayerName(result.EntityId, result.Name, 0, ServerMap.GetName(result.ServerId));
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
            int jobCode = -1;
            var jobCodeVarInt = data.ReadVarInt(afterNameOffset);
            if (jobCodeVarInt.Value < 1) jobCode = jobCodeVarInt.Value;
            else afterNameOffset += jobCodeVarInt.Length;

            int serverId = FindServerId(data, afterNameOffset, endOffset, ServerMap.Servers.Keys.ToHashSet());
            result = new PlayerInfoResult(entityId, nameRead.Value.Name, serverId, jobCode, 0);
            return true;
        }
    }
}
