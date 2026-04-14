using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors.Handlers
{
    internal sealed class BuffPacketHandler(BuffPacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.BUFF_EFFECT;
        public void Handle(PacketProcessor.Packet packet) => processor.Process(packet.Data);
    }
}
