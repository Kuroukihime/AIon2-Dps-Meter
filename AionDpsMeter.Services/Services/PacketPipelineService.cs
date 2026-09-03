using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketCapture;
using AionDpsMeter.Services.PacketProcessing;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.Services
{
    public sealed class PacketPipelineService : IPacketService, IDisposable
    {
        private readonly IPacketCaptureDevice captureDevice;
        private readonly TcpStreamBuffer streamBuffer;
        private readonly EntityTracker entityTracker;
        private readonly PacketProcessor packetProcessor;
        private readonly PacketDispatchService dispatchService;
        private readonly ILogger<PacketPipelineService> logger;

        private bool isRunning;
        private bool disposed;


        public PacketPipelineService(
            IPacketCaptureDevice captureDevice,
            TcpStreamBuffer tcpStreamBuffer,
            EntityTracker entityTracker,
            PacketProcessor packetProcessor,
            PacketDispatchService dispatchService,
            ILogger<PacketPipelineService> logger)
        {
            this.captureDevice = captureDevice;
            this.streamBuffer = tcpStreamBuffer;
            this.entityTracker = entityTracker;
            this.packetProcessor = packetProcessor;
            this.dispatchService = dispatchService;
            this.logger = logger;

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
                var packet = frame;
                packet.ReceivedAt = e.ReceivedAt;
                dispatchService.Dispatch(packet);
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