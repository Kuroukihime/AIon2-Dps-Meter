using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Entity;
using AionDpsMeter.Services.Services.Session;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{

    [PacketOpcode(PacketOpcodes.EntityDeath)]
    public class EntityDeathProcessor(EntityTracker entityTracker, CombatSessionManager sessionManager) : IOpcodeProcessor
    {

        public void Process(Packet packet)
        {
            var reader = new PacketReader(packet.Data);

            reader.ReadVarInt();      // packet length
            reader.ReadU16();         // opcode
            var entityId = (int)reader.ReadVarInt();

            if (entityTracker.IsIdentifiedPlayer(entityId)) sessionManager.RegisterPlayerDeath(entityId);
        }
    }
}
