using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessing.Processors;

namespace AionDpsMeter.Services.PacketProcessing.Handlers
{
    internal sealed class BuffPacketHandler(BuffPacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.BUFF_EFFECT;
        public void Handle(PacketProcessor.Packet packet) => processor.Process(packet.Data);
    }
    internal sealed class MobSummonHandler(MobPacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.MOB_SUMMON;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessMobSpawn(packet.Data);
    }
    internal sealed class RemainHpHandler(RemainHpProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.REMAIN_HP;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessRemainHp(packet.Data);
    }
    internal sealed class PingPacketHandler(ServerTimePacketProcessor processor, Action<int> onPing) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.CURRENT_TIME;
        public void Handle(PacketProcessor.Packet packet)
        {
            var ping = processor.GetPing(packet.Data, packet.ReceivedAt);
            onPing(ping);
        }
    }

    internal sealed class EntityDeathHandler(EntityDeathProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.ENTITY_DEATH;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessEntityDeath(packet.Data);

    }

}
