using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessing.Processors.Damage
{
    internal sealed record ParsedDamagePacket(DamagePacketData? Data, PacketProcessResult Result)
    {
        public bool IsValid => Result == PacketProcessResult.SUCCES;
    }
}
