namespace AionDpsMeter.Services.PacketProcessors.Ping
{
    internal sealed class ServerTimePacketProcessor
    {
        public int GetPing(byte[] packet, long nowMs)
        {
            const int timestampOffset = 5;
            const long dotnetToUnixOffset = 62135596800000;

            if (packet.Length < timestampOffset + 8)
                throw new ArgumentException("Packet too short");

            long dotnetMs = BitConverter.ToInt64(packet, timestampOffset);
            long clientUnixMs = dotnetMs - dotnetToUnixOffset;
            return (int)(nowMs - clientUnixMs);
        }
    }
}
