using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Session;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.BuffEffectA)]
    [PacketOpcode(PacketOpcodes.BuffEffectB)]
    public sealed class BuffPacketProcessor : IOpcodeProcessor
    {

        private const uint MaxReasonableBuffDurationMs = 3_600_000;

        private readonly GameDataProvider gameData;
        private readonly ILogger<BuffPacketProcessor> logger;
        private readonly CombatSessionManager sessionManager;

        public BuffPacketProcessor(ILogger<BuffPacketProcessor> logger, CombatSessionManager sessionManager)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
            this.sessionManager = sessionManager;

        }

        public void Process(Packet packet)
        {
            var r = new PacketReader(packet.Data);

            r.ReadVarInt(); //len
            var opcode = r.ReadU16();    //opcode
            var entityId = (int)r.ReadVarInt();

            if (opcode == PacketOpcodes.BuffEffectA)
            {
                var count = r.ReadU8();
                for (int i = 0; i < count; i++)
                {
                    ProcessBuffEntry(r, entityId);
                }
                return;
            }

            ProcessBuffEntry(r, entityId);

        }

        private void ProcessBuffEntry(PacketReader r, int entityId)
        {

            byte mask = r.ReadU8();
            var castId = r.ReadVarInt();
            var buffId = (int)r.ReadU32() / 10;

            if (!gameData.IsBuff(buffId)) return;
            var skill = gameData.GetSkillOrDefault(buffId);

            var durationMs = r.ReadU64();
            if (durationMs < 100 || durationMs > MaxReasonableBuffDurationMs) return;

            r.ReadU64(); //expiresAt 

            int casterId = 0;

            if ((mask & 0x01) != 0)
            {
                casterId = (int)r.ReadVarInt();
            }
           
            r.ReadU8();
            if ((mask & 0x02) != 0) r.ReadU32(); //GrantedBySkillId
            r.ReadBit();
            if ((mask & 0x04) != 0) r.ReadVarInt(); 
            if ((mask & 0x08) != 0) r.ReadU32(); 
            if ((mask & 0x010) != 0) r.Skip(12); 


            var buffEvent = new BuffEvent
            {
                EntityId = entityId,
                CastId = (int)castId,
                BuffId = skill.Id,
                BuffName = skill.Name,
                BuffIcon = skill.Icon,
                Description = "",
                DurationMs = (uint)durationMs,
                AppliedAt = DateTime.Now,
                CasterId = casterId,
            };

            sessionManager.ProcessBuffEvent(buffEvent);
        }

       
    }
}
