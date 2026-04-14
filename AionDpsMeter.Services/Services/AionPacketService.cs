using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketCapture;
using AionDpsMeter.Services.PacketProcessors;
using AionDpsMeter.Services.PacketProcessors.Handlers;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.Services
{
    public sealed class AionPacketService : IPacketService, IDisposable
    {
        private readonly IPacketCaptureDevice captureDevice;
        private readonly TcpStreamBuffer streamBuffer;
        private readonly EntityTracker entityTracker;
        private readonly PacketProcessor packetProcessor;
        private readonly PacketDispatcher dispatcher;

        private bool isRunning;
        private bool disposed;

        public event EventHandler<PlayerDamage>? DamageReceived;
        public event EventHandler<BuffEvent>? BuffReceived;
        public event EventHandler<int>? PingUpdated;
        public int CurrentPingMs { get; private set; }

        public AionPacketService(
            IPacketCaptureDevice captureDevice,
            TcpStreamBuffer tcpStreamBuffer,
            EntityTracker entityTracker,
            ILoggerFactory loggerFactory)
        {
            this.captureDevice = captureDevice;
            this.streamBuffer = tcpStreamBuffer;
            this.entityTracker = entityTracker;

            packetProcessor = new PacketProcessor(loggerFactory.CreateLogger<PacketProcessor>());

            var nicknameProcessor = new NicknamePacketProcessor(entityTracker, loggerFactory.CreateLogger<NicknamePacketProcessor>());
            var damageProcessor = new DamagePacketProcessor(entityTracker, loggerFactory.CreateLogger<DamagePacketProcessor>());
            var mobProcessor = new MobPacketProcessor(entityTracker, loggerFactory.CreateLogger<MobPacketProcessor>());
            var buffProcessor = new BuffPacketProcessor(loggerFactory.CreateLogger<BuffPacketProcessor>());
            var serverTimeProcessor = new ServerTimePacketProcessor();

            damageProcessor.DamageReceived += (s, e) => DamageReceived?.Invoke(this, e);
            buffProcessor.BuffReceived += (s, e) => BuffReceived?.Invoke(this, e);

            IEnumerable<IPacketHandler> handlers =
            [
                new PlayerInfoHandler(nicknameProcessor),
                new OtherPlayersInfoHandler(nicknameProcessor),
                new PartyInfoHandler(nicknameProcessor),
                new GlobalSessIdLinkingHandler(nicknameProcessor),
                new DamagePacketHandler(damageProcessor),
                new DotDamagePacketHandler(damageProcessor),
                new MobHpHandler(mobProcessor),
                new MobSummonHandler(mobProcessor),
                new BuffPacketHandler(buffProcessor),
                new PingPacketHandler(serverTimeProcessor, ping =>
                {
                    CurrentPingMs = ping;
                    PingUpdated?.Invoke(this, ping);
                }),
            ];

            dispatcher = new PacketDispatcher(handlers, loggerFactory.CreateLogger<PacketDispatcher>());
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
                dispatcher.Dispatch(f);
            }
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
