using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.GlobalSessIdLinking)]
    internal class GlobalIdLinkingProcessor(EntityTracker entityTracker) : IOpcodeProcessor
    {
        public void Process(Packet packet)
        {

            var r = new PacketReader(packet.Data);

            r.ReadVarInt();
            r.ReadU16();
            r.Skip(2);
            var playerId = (int)r.ReadVarInt();
            r.Skip(4);
            var globalId = (int)r.ReadU32();

            entityTracker.LinkSessionToGlobalPlayer(globalId, playerId);
        }
    }
}
