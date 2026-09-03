using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.PacketProcessing.Routing;
using K4os.Compression.LZ4;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessing
{
    public sealed class PacketProcessor(ILogger<PacketProcessor> logger)
    {
        
        internal List<Packet> ProcessPacket(byte[] packet)
        {
            try
            {
                if (!IsCompressedStream(packet))
                    return [new Packet { Data = packet }];

                return ExtractInnerPackets(packet);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return [];
            }
        }

        private static bool IsCompressedStream(byte[] packet)
        {
            if (packet.Length < 3) return false;

            var lenVarInt = packet.ReadVarInt();
            if (lenVarInt.Length <= 0) return false;

            int opcodeOffset = lenVarInt.Length;
            return packet.Length >= opcodeOffset + 2
                   && packet[opcodeOffset] == 0xFF
                   && packet[opcodeOffset + 1] == 0xFF;
        }

        private List<Packet> ExtractInnerPackets(byte[] rawPacket)
        {
            var result = new List<Packet>();
            var stack = new Stack<(byte[] Buffer, int Offset, int Length)>();
            stack.Push((rawPacket, 0, rawPacket.Length));

            while (stack.Count > 0)
            {
                var (buf, offset, length) = stack.Pop();

                foreach (var frame in ScanFrames(buf.AsSpan(), offset, length))
                {
                    try
                    {
                        if (TryDecompress(
                                buf.AsSpan(),
                                frame.FrameBase,
                                frame.FramePayloadLen,
                                frame.VarintLen,
                                out var decompressed,
                                out int decompressedLen))
                        {
                            stack.Push((decompressed!, 0, decompressedLen));
                        }
                        else
                        {
                            CollectPlainFrame(buf, frame, result);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                    }
                }
            }

            return result;
        }

        private static void CollectPlainFrame(byte[] buf, FrameInfo frame, List<Packet> result)
        {
            int dataLen = frame.FramePayloadLen - frame.VarintLen;
            if (dataLen <= 0) return;

            byte[] frameBytes = buf.AsSpan(frame.FrameBase, dataLen + frame.VarintLen).ToArray();
            result.Add(new Packet { Data = frameBytes });
        }

        private readonly record struct FrameInfo(int FrameBase, int FramePayloadLen, int VarintLen);

        private static List<FrameInfo> ScanFrames(ReadOnlySpan<byte> data, int offset, int length)
        {
            var frames = new List<FrameInfo>();
            int end = offset + length;
            int pos = offset;

            while (pos < end)
            {
                if (data[pos] == 0x00)
                {
                    pos++;
                    continue;
                }

                var varintVal = data.ReadVarInt(pos);
                int varint = varintVal.Value;
                int varintLen = varintVal.Length;

                if (varintLen <= 0) break;
                if (varint > 2_000_000) break;

                int framePayloadLen = varint + varintLen - 4;
                if (framePayloadLen <= 0)
                {
                    pos++;
                    continue;
                }

                int frameEnd = pos + framePayloadLen;
                if (frameEnd > end) break;

                frames.Add(new FrameInfo(pos, framePayloadLen, varintLen));
                pos = frameEnd;
            }

            return frames;
        }

        private static bool TryDecompress(
            ReadOnlySpan<byte> raw,
            int frameBase,
            int framePayloadLen,
            int varintLen,
            out byte[]? decompressed,
            out int decompressedLen)
        {
            decompressed = null;
            decompressedLen = 0;

            int headerOffset = varintLen;

            if (headerOffset < framePayloadLen)
            {
                byte flagByte = raw[frameBase + headerOffset];
                if ((flagByte & 0xF0) == 0xF0 && flagByte != 0xFF)
                    headerOffset++;
            }

            if (framePayloadLen < headerOffset + 2) return false;
            if (raw[frameBase + headerOffset] != 0xFF || raw[frameBase + headerOffset + 1] != 0xFF)
                return false;

            if (framePayloadLen < headerOffset + 6) return false;

            int decompBase = frameBase + headerOffset;
            int size =
                raw[decompBase + 2]
                | (raw[decompBase + 3] << 8)
                | (raw[decompBase + 4] << 16)
                | (raw[decompBase + 5] << 24);

            if ((uint)(size - 1) > 0x98967F) return false;

            int compPayloadOffset = headerOffset + 6;
            int compPayloadLen = framePayloadLen - compPayloadOffset;
            if (compPayloadLen <= 0) return false;

            byte[] output = new byte[size];
            int actual = LZ4Codec.Decode(
                raw.Slice(frameBase + compPayloadOffset, compPayloadLen),
                output.AsSpan());

            if (actual <= 0) return false;

            decompressed = output;
            decompressedLen = actual;
            return true;
        }
    }
}
