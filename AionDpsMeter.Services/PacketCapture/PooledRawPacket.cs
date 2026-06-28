using System.Buffers;
using PacketDotNet;

namespace AionDpsMeter.Services.PacketCapture;

internal sealed class PooledRawPacket : IDisposable
{
    public LinkLayers LinkLayerType { get; }
    public byte[] Buffer { get; }
    public int Length { get; }
    public DateTime Timestamp { get; }

    public AdapterContext? SourceAdapter { get; }

    private bool disposed;

    public PooledRawPacket(LinkLayers linkLayerType, byte[] buffer, int length, DateTime timestamp, AdapterContext? sourceAdapter = null)
    {
        LinkLayerType = linkLayerType;
        Buffer = buffer;
        Length = length;
        Timestamp = timestamp;
        SourceAdapter = sourceAdapter;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ArrayPool<byte>.Shared.Return(Buffer);
    }
}
