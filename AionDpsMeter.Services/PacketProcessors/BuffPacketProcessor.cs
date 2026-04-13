using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class BuffPacketProcessor
    {
        public event EventHandler<BuffEvent>? BuffReceived;

        private const uint MaxReasonableBuffDurationMs = 3_600_000; // 1 hour

        private readonly GameDataProvider gameData;
        private readonly EntityTracker entityTracker;
        private readonly ILogger<BuffPacketProcessor> logger;

        public BuffPacketProcessor(EntityTracker entityTracker, ILogger<BuffPacketProcessor> logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
        }

        public void Process(byte[] packet)
        {
            int offset = 0;
            int packetEnd = packet.Length;

            // Skip varint length header + 2-byte opcode
            var packetLenVarInt = packet.ReadVarInt(offset);
            if (packetLenVarInt.Length <= 0)
                return;
            offset += packetLenVarInt.Length + 2;

            if (offset >= packetEnd)
                return;

            // entityId
            var entityIdVarInt = packet.ReadVarInt(offset);
            if (entityIdVarInt.Length <= 0 || entityIdVarInt.Value < 0)
                return;
            offset += entityIdVarInt.Length;
            int entityId = entityIdVarInt.Value;

            if (offset >= packetEnd)
                return;

            // skip 1 unknown byte, then read type byte
            offset++;
            if (offset >= packetEnd)
                return;

            byte type = packet[offset++];

         

            // skip one more varint (unknown field)
            var unknownVarInt = packet.ReadVarInt(offset);
            if (unknownVarInt.Length <= 0)
                return;
            offset += unknownVarInt.Length;

            if (offset >= packetEnd)
                return;

            // buffId — 4 bytes LE
            if (offset + 4 > packetEnd)
                return;

            int buffId = packet.ReadUInt32Le(offset);
            offset += 4;
            
            if (!gameData.IsBuff(buffId))
            {
                return;
            }
            var skill = gameData.GetBuff(buffId);
            if (skill ==  null)
            {
                return;
            }
            // durationMs — 4 bytes LE
            if (offset + 4 > packetEnd)
                return;

            uint durationMs = (uint)packet.ReadUInt32Le(offset);
            offset += 4;

            if (durationMs < 100 || durationMs > MaxReasonableBuffDurationMs)
                return;

            // skip 4 unknown bytes
            if (offset + 4 > packetEnd)
                return;
            offset += 4;

            // timestamp — 8 bytes LE
            if (offset + 8 > packetEnd)
                return;

            long timestamp = BitConverter.ToInt64(packet, offset);
            offset += 8;

            // casterId — optional trailing varint
            int casterId = 0;
            if (offset < packetEnd)
            {
                var casterVarInt = packet.ReadVarInt(offset);
                if (casterVarInt.Length > 0 && casterVarInt.Value >= 0)
                    casterId = casterVarInt.Value;
            }

            logger.LogInformation($"[BUFF] entityId={entityId} buffId={buffId} buffName={skill.Name} type={type} durationMs={durationMs} casterId={casterId}");

            string? iconUrl = !string.IsNullOrWhiteSpace(skill.IconUrlPart)
                ? $"https://assets.playnccdn.com/static-aion2-gamedata/resources/{skill.IconUrlPart}"
                : null;

            BuffReceived?.Invoke(this, new BuffEvent
            {
                EntityId = entityId,
                BuffId = buffId,
                BuffName = skill.Name,
                BuffIcon = iconUrl,
                Description = skill.Description,
                DurationMs = durationMs,
                AppliedAt = DateTime.Now,
                CasterId = casterId,
            });
        }
    }
}