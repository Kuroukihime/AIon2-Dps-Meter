using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessors.Mob;

namespace AionDpsMeter.Services.PacketProcessors.Handlers
{
    internal sealed class MobHpHandler(MobPacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.MOB_HP;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessMobHp(packet.Data);
    }

    internal sealed class MobSummonHandler(MobPacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.MOB_SUMMON;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessMobSpawn(packet.Data);
    }
}
