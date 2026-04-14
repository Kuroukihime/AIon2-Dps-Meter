using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessors.Damage;
using AionDpsMeter.Services.PacketProcessors.DotDamage;

namespace AionDpsMeter.Services.PacketProcessors.Handlers
{
    internal sealed class DamagePacketHandler(DamagePacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.DAMAGE;
        public void Handle(PacketProcessor.Packet packet) => processor.Process(packet.Data);
    }

    internal sealed class DotDamagePacketHandler(DotDamagePacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.DOT_DAMAGE;
        public void Handle(PacketProcessor.Packet packet) => processor.Process(packet.Data);
    }
}
