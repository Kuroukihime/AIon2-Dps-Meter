namespace AionDpsMeter.Services.PacketProcessors.PlayerEntity.GamePackets
{
    public sealed class PartyPacket
    {
        public uint PartyKey { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public byte PartySize { get; set; }
        public uint DungeonId { get; set; }
        public ulong LeaderDbid { get; set; }
        public uint LeaderId => (uint)(LeaderDbid & 0xFFFFFFFF);
        public int LeaderServerId => (int)((LeaderDbid >> 48) & 0xFFFF);

        public List<PartyPlayerPacket> Members { get; } = new List<PartyPlayerPacket>();

        public IEnumerable<PartyPlayerPacket> ValidMembers
        {
            get
            {
                foreach (var m in Members)
                    if (m.IsValid) yield return m;
            }
        }
    }
}
