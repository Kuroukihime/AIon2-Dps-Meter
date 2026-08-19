using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessing
{
    internal interface IPacketHandler
    {
        PacketTypeEnum PacketType { get; }
        void Handle(PacketProcessor.Packet packet);
    }
}
