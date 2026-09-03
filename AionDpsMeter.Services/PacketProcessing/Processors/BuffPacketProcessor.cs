using AionDpsMeter.Core.Data;
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
            r.ReadU16();    //opcode

            var entityId = (int)r.ReadVarInt();
            r.ReadU8();     //unknown
            byte type = r.ReadU8();
            r.ReadVarInt(); //unknown
            var buffId =(int)r.ReadU32() / 10;

            if (!gameData.IsBuff(buffId)) return;
            var skill = gameData.GetSkillOrDefault(buffId);

            var durationMs = r.ReadU32();
            if (durationMs < 100 || durationMs > MaxReasonableBuffDurationMs) return;

            r.Skip(4); //unknown data

            r.ReadU32(); //timestamp

            var casterId = (int)r.ReadVarInt();

            logger.LogTrace("[BUFF] entityId={EntityId} buffId={BuffId} buffName={BuffName} type={Type} durationMs={DurationMs} casterId={CasterId}",
                entityId, buffId, skill.Name, type, durationMs, casterId);

            var buffEvent = new BuffEvent
            {
                EntityId = entityId,
                BuffId = skill.Id,
                BuffName = skill.Name,
                BuffIcon = skill.Icon,
                Description = "",
                DurationMs = durationMs,
                AppliedAt = DateTime.Now,
                CasterId = casterId,
            };

            sessionManager.ProcessBuffEvent(buffEvent);
        }

       
    }
}
