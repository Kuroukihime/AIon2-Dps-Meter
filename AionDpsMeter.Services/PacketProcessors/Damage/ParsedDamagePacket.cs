using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors.Damage
{
    internal sealed record ParsedDamagePacket(DamagePacketData? Data, PacketProcessResult Result)
    {
        public bool IsValid => Result == PacketProcessResult.SUCCES;
    }
}
