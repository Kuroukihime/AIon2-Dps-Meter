using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    internal class RemainHpProcessor(EntityTracker entityTracker)
    {

        public void ProcessRemainHp(byte[] data)
        {
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
