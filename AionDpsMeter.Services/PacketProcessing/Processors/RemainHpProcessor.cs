using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    internal class RemainHpProcessor(EntityTracker entityTracker)
    {
        public event EventHandler<int>? OnPlayerDeath;

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

            if (entityTracker.IsIndentifiedPlayer(entityIdInfo.Value)) ProcessPlayerHp(entityIdInfo.Value, hpCurrent);

        }

        private void ProcessPlayerHp(int playerId, int hpCurrent)
        {
            if (hpCurrent != 0) return;
            OnPlayerDeath?.Invoke(this, playerId);
        }
    }
}
