using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.RemainHp)]
    internal class RemainHpProcessor(EntityTracker entityTracker) : IOpcodeProcessor
    {
        public void Process(Packet packet)
        {
            var data = packet.Data;
            int offset = 3;
            var entityIdInfo = data.ReadVarInt(offset);
            offset += entityIdInfo.Length;
            offset += data.ReadVarInt(offset).Length;
            offset += data.ReadVarInt(offset).Length;
            offset += data.ReadVarInt(offset).Length;
            var hpCurrent = data.ReadUInt32Le(offset);
            entityTracker.UpdateTargetEntityHpCurrent(entityIdInfo.Value, hpCurrent);
        }

    }
}
