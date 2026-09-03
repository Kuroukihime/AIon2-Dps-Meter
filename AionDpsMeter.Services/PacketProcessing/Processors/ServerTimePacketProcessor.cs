using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.Services.Session;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.ServerTime)]
    public sealed class ServerTimePacketProcessor(CombatSessionManager sessionManager) : IOpcodeProcessor
    {
        public void Process(Packet packet)
        {
            const int timestampOffset = 5;
            const long dotnetToUnixOffset = 62135596800000;

            if (packet.Data.Length < timestampOffset + 8)
                throw new ArgumentException("Packet too short");

            long dotnetMs = BitConverter.ToInt64(packet.Data, timestampOffset);
            long clientUnixMs = dotnetMs - dotnetToUnixOffset;
            var ms = (int)(packet.ReceivedAt - clientUnixMs);
            sessionManager.FirePingUpdate(ms);
        }
    }
}
