using AionDpsMeter.Services.Models;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal sealed class PacketDispatcher
    {
        private readonly Dictionary<PacketTypeEnum, IPacketHandler> handlers;
        private readonly ILogger<PacketDispatcher> logger;

        public PacketDispatcher(IEnumerable<IPacketHandler> handlers, ILogger<PacketDispatcher> logger)
        {
            this.logger = logger;
            this.handlers = handlers.ToDictionary(h => h.PacketType);
        }

        public void Dispatch(PacketProcessor.Packet packet)
        {
            try
            {
                if (handlers.TryGetValue(packet.Type, out var handler))
                    handler.Handle(packet);
                else
                    logger.LogTrace("UNKNOWN PACKET TYPE {packetType}", packet.Type);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing packet of type {packetType}", packet.Type);
            }
        }
    }
}
