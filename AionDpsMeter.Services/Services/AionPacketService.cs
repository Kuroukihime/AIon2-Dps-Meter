using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.PacketCapture;
using AionDpsMeter.Services.PacketProcessors;
using Microsoft.Extensions.Logging;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services
{
    public sealed class AionPacketService : IPacketService, IDisposable
    {
        private readonly IPacketCaptureDevice captureDevice;
        private readonly TcpStreamBuffer streamBuffer;
        private readonly EntityTracker entityTracker;

        private bool isRunning;
        private bool disposed;

        private readonly PacketProcessor packetProcessor;
        private readonly NicknamePacketProcessor nicknameProcessor;
        private readonly DamagePacketProcessor damagePacketProcessor;
        private readonly ServerTimePacketProcessor serverTimePacketProcessor;
        private readonly MobPacketProcessor mobPacketProcessor;
        private readonly BuffPacketProcessor buffPacketProcessor;

        public event EventHandler<PlayerDamage>? DamageReceived;
        public event EventHandler<BuffEvent>? BuffReceived;
        private ILogger<AionPacketService> logger;

        public event EventHandler<int>? PingUpdated;
        public int CurrentPingMs { get; private set; }

        private const uint MaxReasonableBuffDurationMs = 3_600_000; // 1 hour

        public AionPacketService(IPacketCaptureDevice captureDevice, TcpStreamBuffer tcpStreamBuffer, EntityTracker entityTracker, ILoggerFactory loggerFactory)
        {
            this.captureDevice = captureDevice;
            logger = loggerFactory.CreateLogger<AionPacketService>();
            streamBuffer = tcpStreamBuffer;
            this.entityTracker = entityTracker;

            packetProcessor = new PacketProcessor(loggerFactory.CreateLogger<PacketProcessor>());
            nicknameProcessor = new NicknamePacketProcessor(entityTracker, loggerFactory.CreateLogger<NicknamePacketProcessor>());
            damagePacketProcessor = new DamagePacketProcessor(entityTracker, loggerFactory.CreateLogger<DamagePacketProcessor>());
            serverTimePacketProcessor = new();
            mobPacketProcessor = new MobPacketProcessor(entityTracker, loggerFactory.CreateLogger<MobPacketProcessor>());
            buffPacketProcessor = new BuffPacketProcessor(entityTracker, loggerFactory.CreateLogger<BuffPacketProcessor>());

            damagePacketProcessor.DamageReceived += (s, e) => DamageReceived?.Invoke(this, e);
            buffPacketProcessor.BuffReceived += OnBuffReceived;
            streamBuffer.PacketExtracted += OnPacketExtracted;

        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;
            captureDevice.StartCapture();
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            captureDevice.StopCapture();
        }

        public void Reset()
        {
            entityTracker.Clear();
            streamBuffer.Clear();
        }


        private void OnPacketExtracted(object? sender, TcpPacketEventArgs e)
        {
            var frames = packetProcessor.ProcessPacket(e.Payload);

            foreach (var frame in frames)
            {
                var f = frame;
                f.ReceivedAt = e.ReceivedAt;
                ProcessFrame(f);
            }
        }

        private void ProcessFrame(PacketProcessor.Packet packet)
        {
            try
            {
                nicknameProcessor.Process(packet.Data);

                if (packet.Type == PacketTypeEnum.DAMAGE) damagePacketProcessor.Process04_38(packet.Data);
                else if (packet.Type == PacketTypeEnum.DOT_DAMAGE) damagePacketProcessor.ProcessDotDamage(packet.Data);
                else if (packet.Type == PacketTypeEnum.CURRENT_TIME) ProcessPing(packet);
                else if (packet.Type == PacketTypeEnum.MOB_HP) mobPacketProcessor.ProcessMobHp(packet.Data);
                else if (packet.Type == PacketTypeEnum.MOB_SUMMON) mobPacketProcessor.ProcessMobSpawn(packet.Data);
                else if (packet.Type == PacketTypeEnum.BUFF_EFFECT) buffPacketProcessor.Process(packet.Data);
                else
                {
                    logger.LogTrace("UNKNOWN PACKET TYPE {packetType}", packet.Type);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing packet of type {packetType}", packet.Type);
            }
        }

        private void OnBuffReceived(object? sender, BuffEventArgs e)
        {
            // Filter out permanent, zero-duration, and very long buffs
            if (e.DurationMs == uint.MaxValue) return;
            if (e.DurationMs == 0) return;
            if (e.DurationMs > MaxReasonableBuffDurationMs) return;

            var gameData = GameDataProvider.Instance;
            var buffData = gameData.GetBuff(e.BuffId);
            if (buffData is null) return;

            string? iconUrl = !string.IsNullOrWhiteSpace(buffData.IconUrlPart)
                ? $"https://assets.playnccdn.com/static-aion2-gamedata/resources/{buffData.IconUrlPart}"
                : null;

            var buffEvent = new BuffEvent
            {
                EntityId = e.EntityId,
                BuffId = e.BuffId,
                BuffName = buffData.Name,
                BuffIcon = iconUrl,
                Description = buffData.Description,
                DurationMs = e.DurationMs,
                AppliedAt = DateTime.Now,
                CasterId = e.CasterId,
            };

            BuffReceived?.Invoke(this, buffEvent);
        }

        private void ProcessPing(PacketProcessor.Packet packet)
        {
            CurrentPingMs = serverTimePacketProcessor.GetPing(packet.Data, packet.ReceivedAt);
            PingUpdated?.Invoke(this, CurrentPingMs);
        }

        public void Dispose()
        {
            if (disposed) return;

            Stop();
            streamBuffer.PacketExtracted -= OnPacketExtracted;
            captureDevice.Dispose();

            disposed = true;
        }
    }
}
