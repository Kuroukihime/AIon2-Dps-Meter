using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessors.PlayerEntity;

namespace AionDpsMeter.Services.PacketProcessors.Handlers
{
    internal sealed class PlayerInfoHandler(PlayerProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.PLAYER_INFO;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessPlayerInfo(packet.Data);
    }

    internal sealed class OtherPlayersInfoHandler(PlayerProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.OTHER_PLAYERS_INFO;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessOtherPlayersInfo(packet.Data);
    }

    internal sealed class PartyInfoHandler(PlayerProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.PARTY_INFO;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessPartyPacket(packet.Data);
    }

    internal sealed class GlobalSessIdLinkingHandler(PlayerProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.GLOBAL_SESSID_LINKING;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessGlobalSessionIdLinking(packet.Data);
    }
}
