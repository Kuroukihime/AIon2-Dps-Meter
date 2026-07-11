using AionDpsMeter.Core.Data;

namespace AionDpsMeter.Services.PacketProcessors.PlayerEntity.GamePackets
{

    /// <summary>One party member slot.</summary>
    public sealed class PartyPlayerPacket
    {
        /// <summary>1-based party slot number (_number).</summary>
        public byte SlotNumber { get; set; }

        /// <summary>Raw 64-bit GlobalID</summary>
        public ulong Dbid { get; set; }

        /// <summary>Low 32 bits of Dbid — the character id.</summary>
        public uint Id => (uint)(Dbid & 0xFFFFFFFF);

        /// <summary>Top 16 bits of Dbid — the server id.</summary>
        public int ServerId => (int)((Dbid >> 48) & 0xFFFF);

        public string ServerName => ServerMap.GetName(ServerId);

        public string Name { get; set; } = string.Empty;

        /// <summary>_level, always present.</summary>
        public uint CharactedLevel { get; set; }

        /// <summary>_equip_item_level, only present when presence_mask &amp; 0x04.</summary>
        public uint? GearScore { get; set; }

        /// <summary>_combat_power, only present when presence_mask &amp; 0x20.</summary>
        public ulong? CombatPower { get; set; }

        /// <summary>Raw presence_mask byte, kept for debugging/future field decoding.</summary>
        public byte PresenceMask { get; set; }

        /// <summary>Any player slot that is not empty.</summary>
        public bool IsValid => PresenceMask != 0;
    }
}
