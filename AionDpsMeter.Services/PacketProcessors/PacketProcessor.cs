using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;
using K4os.Compression.LZ4;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class PacketProcessor(ILogger<PacketProcessor> logger)
    {
        internal struct Packet
        {
            public PacketTypeEnum Type;
            public byte[] Data;
            public long ReceivedAt;
        }

        private static readonly Dictionary<(byte, byte), PacketTypeEnum> OpcodeMap = new()
        {
            { (0x04, 0x38), PacketTypeEnum.DAMAGE },
            { (0x05, 0x38), PacketTypeEnum.DOT_DAMAGE },
            { (0xFF, 0xFF), PacketTypeEnum.COMPRESSED_STREAM },
            { (0x03, 0x36), PacketTypeEnum.CURRENT_TIME },
            { (0x00, 0x8D), PacketTypeEnum.MOB_HP },
            { (0x40, 0x36), PacketTypeEnum.MOB_SUMMON },
            { (0x2A, 0x38), PacketTypeEnum.BUFF_EFFECT },
            { (0x2B, 0x38), PacketTypeEnum.BUFF_EFFECT },
            { (0x33, 0x36), PacketTypeEnum.PLAYER_INFO },
            { (0x44, 0x36), PacketTypeEnum.OTHER_PLAYERS_INFO },
            { (0x20, 0x36), PacketTypeEnum.GLOBAL_SESSID_LINKING },
            { (0x02, 0x97), PacketTypeEnum.PARTY_INFO },
        };

        internal List<Packet> ProcessPacket(byte[] packet)
        {
            try
            {
                var type = DeterminePacketType(packet);

                if (type != PacketTypeEnum.COMPRESSED_STREAM)
                    return [new Packet { Type = type, Data = packet }];

                return ExtractInnerPackets(packet);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return [];
            }
        }

        private static PacketTypeEnum DeterminePacketType(byte[] packet)
        {
            var lenValueLength = packet.ReadVarInt().Length;
            if (lenValueLength < 0 || packet.Length < lenValueLength + 2)
                return PacketTypeEnum.BROKEN;

            var key = (packet[lenValueLength], packet[lenValueLength + 1]);
            return OpcodeMap.GetValueOrDefault(key, PacketTypeEnum.UNKNOWN);
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
                        if (TryDecompress(buf.AsSpan(), frame.FrameBase, frame.FramePayloadLen,
                                frame.VarintLen, out byte[]? decompressed, out int decompressedLen))
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

        private void CollectPlainFrame(byte[] buf, FrameInfo frame, List<Packet> result)
        {
            int dataLen = frame.FramePayloadLen - frame.VarintLen;
            if (dataLen <= 0) return;

            byte[] frameBytes = buf.AsSpan(frame.FrameBase, dataLen + frame.VarintLen).ToArray();
            result.Add(new Packet { Type = DeterminePacketType(frameBytes), Data = frameBytes });
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
            int frameBase, int framePayloadLen, int varintLen,
            out byte[]? decompressed, out int decompressedLen)
        {
            decompressed = null;
            decompressedLen = 0;

            // Step 1: optional flag-byte skip
            int headerOffset = varintLen;

            if (headerOffset < framePayloadLen)
            {
                byte flagByte = raw[frameBase + headerOffset];
                if ((flagByte & 0xF0) == 0xF0 && flagByte != 0xFF)
                    headerOffset++;
            }

            // Step 2: check for 0xFF 0xFF compressed marker
            if (framePayloadLen < headerOffset + 2) return false;
            if (raw[frameBase + headerOffset] != 0xFF || raw[frameBase + headerOffset + 1] != 0xFF)
                return false;

            // Step 3: read 4-byte LE decompressed size
            if (framePayloadLen < headerOffset + 6) return false;

            int decompBase = frameBase + headerOffset;
            int size =
                raw[decompBase + 2]
                | (raw[decompBase + 3] << 8)
                | (raw[decompBase + 4] << 16)
                | (raw[decompBase + 5] << 24);

            if ((uint)(size - 1) > 0x98967F) return false;

            // Step 4: locate compressed payload
            int compPayloadOffset = headerOffset + 6;
            int compPayloadLen = framePayloadLen - compPayloadOffset;
            if (compPayloadLen <= 0) return false;

            // Step 5: decompress
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
