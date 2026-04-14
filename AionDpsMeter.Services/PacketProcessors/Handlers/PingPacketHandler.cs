using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors.Handlers
{
    internal sealed class PingPacketHandler(ServerTimePacketProcessor processor, Action<int> onPing) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.CURRENT_TIME;
        public void Handle(PacketProcessor.Packet packet)
        {
            var ping = processor.GetPing(packet.Data, packet.ReceivedAt);
            onPing(ping);
        }
    }
}
