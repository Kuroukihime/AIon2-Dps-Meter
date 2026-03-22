using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AionDpsMeter.Services.PacketCapture
{

    public sealed class TcpStreamBuffer
    {
        private readonly ConcurrentDictionary<string, PacketAccumulator> _streamAccumulators = new();
        public event EventHandler<TcpPacketEventArgs>? PacketExtracted;
        private readonly ILogger<TcpStreamBuffer> logger;
        private readonly FilePacketWriter? filePacketWriter;

        public TcpStreamBuffer(ILogger<TcpStreamBuffer> logger, FilePacketWriter? filePacketWriter = null)
        {
            this.logger = logger;
            this.filePacketWriter = filePacketWriter;
        }

        public void AddData(string streamKey, byte[] payload, long arrivedAt)
        {
            var accumulator = _streamAccumulators.GetOrAdd(streamKey, _ => new PacketAccumulator(logger));
            filePacketWriter?.LogPacket(streamKey, payload);
            accumulator.AppendAndProcess(payload, (packet) =>
            {
                PacketExtracted?.Invoke(this, new TcpPacketEventArgs
                {
                    StreamKey = streamKey,
                    Payload = packet,
                    ReceivedAt = arrivedAt
                });
            });
        }

        public void Clear()
        {
            foreach (var accumulator in _streamAccumulators.Values) accumulator.Clear();
            _streamAccumulators.Clear();
            filePacketWriter?.FinalizePacketLogging();
        }

        public void ClearStream(string streamKey)
        {
            if (_streamAccumulators.TryRemove(streamKey, out var accumulator)) accumulator.Clear();
        }

        public int ActiveStreamsCount => _streamAccumulators.Count;
    }
}