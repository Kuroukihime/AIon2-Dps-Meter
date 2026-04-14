using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessors.Nickname;

namespace AionDpsMeter.Services.PacketProcessors.Handlers
{
    internal sealed class PlayerInfoHandler(NicknamePacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.PLAYER_INFO;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessPlayerInfo(packet.Data);
    }

    internal sealed class OtherPlayersInfoHandler(NicknamePacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.OTHER_PLAYERS_INFO;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessOtherPlayersInfo(packet.Data);
    }

    internal sealed class PartyInfoHandler(NicknamePacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.PARTY_INFO;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessPartyPacket(packet.Data);
    }

    internal sealed class GlobalSessIdLinkingHandler(NicknamePacketProcessor processor) : IPacketHandler
    {
        public PacketTypeEnum PacketType => PacketTypeEnum.GLOBAL_SESSID_LINKING;
        public void Handle(PacketProcessor.Packet packet) => processor.ProcessGlobalSessionIdLinking(packet.Data);
    }
}
