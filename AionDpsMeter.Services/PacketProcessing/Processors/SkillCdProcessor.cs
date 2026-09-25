using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Timed;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.SkillCd)]
    internal class SkillCdProcessor(CombatSessionManager sessionManager) : IOpcodeProcessor
    {
        private readonly GameDataProvider gameData = GameDataProvider.Instance;
        public void Process(Packet packet)
        {
            var r = new PacketReader(packet.Data);

            r.ReadVarInt(); //len
            r.ReadU16(); //opcode

            var count = r.ReadU8();

            for (int i = 0; i < count; i++)
            {
                ProcessEntry(r);
            }
        }
       
        private void ProcessEntry(PacketReader r)
        {
            var skillId = r.ReadU32();
            var msLeft = r.ReadVarInt();
            var skill = gameData.GetSkillOrDefault((int)skillId);

            sessionManager.RegisterSkillCdEvent(new TimedEvent()
            {
                Name = skill.Name,
                IconUrl = skill.Icon ?? string.Empty,
                Id = (uint)skill.Id,
                Duration = TimeSpan.FromMilliseconds(msLeft)
            });

        }

    }
}
