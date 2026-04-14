using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Extensions;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors.Buff
{
    internal sealed class BuffPacketProcessor
    {
        public event EventHandler<BuffEvent>? BuffReceived;

        private const uint MaxReasonableBuffDurationMs = 3_600_000;

        private readonly GameDataProvider gameData;
        private readonly ILogger<BuffPacketProcessor> logger;

        public BuffPacketProcessor(ILogger<BuffPacketProcessor> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
        }

        public void Process(byte[] packet)
        {
            int offset = 0;
            int packetEnd = packet.Length;

            var packetLenVarInt = packet.ReadVarInt(offset);
            if (packetLenVarInt.Length <= 0) return;
            offset += packetLenVarInt.Length + 2;
            if (offset >= packetEnd) return;

            var entityIdVarInt = packet.ReadVarInt(offset);
            if (entityIdVarInt.Length <= 0 || entityIdVarInt.Value < 0) return;
            offset += entityIdVarInt.Length;
            int entityId = entityIdVarInt.Value;
            if (offset >= packetEnd) return;

            offset++; // skip unknown byte
            if (offset >= packetEnd) return;
            byte type = packet[offset++];

            var unknownVarInt = packet.ReadVarInt(offset);
            if (unknownVarInt.Length <= 0) return;
            offset += unknownVarInt.Length;
            if (offset >= packetEnd) return;

            if (offset + 4 > packetEnd) return;
            int buffId = packet.ReadUInt32Le(offset);
            offset += 4;

            if (!gameData.IsBuff(buffId)) return;
            var skill = gameData.GetBuff(buffId);
            if (skill == null) return;

            if (offset + 4 > packetEnd) return;
            uint durationMs = (uint)packet.ReadUInt32Le(offset);
            offset += 4;
            if (durationMs < 100 || durationMs > MaxReasonableBuffDurationMs) return;

            if (offset + 4 > packetEnd) return;
            offset += 4; // skip unknown bytes

            if (offset + 8 > packetEnd) return;
            offset += 8; // skip timestamp

            int casterId = 0;
            if (offset < packetEnd)
            {
                var casterVarInt = packet.ReadVarInt(offset);
                if (casterVarInt.Length > 0 && casterVarInt.Value >= 0)
                    casterId = casterVarInt.Value;
            }

            logger.LogTrace("[BUFF] entityId={EntityId} buffId={BuffId} buffName={BuffName} type={Type} durationMs={DurationMs} casterId={CasterId}",
                entityId, buffId, skill.Name, type, durationMs, casterId);

            BuffReceived?.Invoke(this, new BuffEvent
            {
                EntityId = entityId,
                BuffId = buffId,
                BuffName = skill.Name,
                BuffIcon = skill.Icon,
                Description = skill.Description,
                DurationMs = durationMs,
                AppliedAt = DateTime.Now,
                CasterId = casterId,
            });
        }
    }
}
