using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal interface IPacketHandler
    {
        PacketTypeEnum PacketType { get; }
        void Handle(PacketProcessor.Packet packet);
    }
}
