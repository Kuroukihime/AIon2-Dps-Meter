using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors.Handlers
{
    internal sealed class DamagePacketHandler(DamagePacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.DAMAGE;
        public void Handle(PacketProcessor.Packet packet) => processor.Process04_38(packet.Data);
    }

    internal sealed class DotDamagePacketHandler(DamagePacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.DOT_DAMAGE;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessDotDamage(packet.Data);
    }
}
