using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    
    internal class EntityDeathProcessor(EntityTracker entityTracker)
    {
        public event EventHandler<int>? OnPlayerDeath;

        public void ProcessEntityDeath(byte[] data)
        {
            var reader = new PacketReader(data);

            reader.ReadVarInt();      // packet length
            reader.ReadU16();         // opcode
            var entityId = (int)reader.ReadVarInt();      

            if (entityTracker.IsIdentifiedPlayer(entityId)) OnPlayerDeath?.Invoke(this, entityId);
        }

    }
}
