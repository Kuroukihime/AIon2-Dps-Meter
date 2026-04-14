using AionDpsMeter.Services.Extensions;

namespace AionDpsMeter.Services.PacketProcessors.Shared
{
   
    internal ref struct PacketReader
    {
        private readonly byte[] data;
        private int offset;

        public int Offset => offset;

        public PacketReader(byte[] data, int startOffset = 0)
        {
            this.data = data;
            this.offset = startOffset;
        }

        /// <summary>Reads the varint length prefix and validates the 2-byte opcode.</summary>
        public bool ReadAndValidateHeader(byte opcode1, byte opcode2)
        {
            if (!HasRemainingBytes()) return false;

            var packetLengthInfo = data.ReadVarInt();
            if (packetLengthInfo.Length < 0) return false;

            offset += packetLengthInfo.Length;

            if (!HasRemainingBytes(2)) return false;
            if (data[offset] != opcode1 || data[offset + 1] != opcode2) return false;

            offset += 2;
            return HasRemainingBytes();
        }

        /// <summary>Reads a positive varint entity id.</summary>
        public bool ReadVarIntId(out int id)
        {
            id = 0;
            var (value, bytes) = data.ReadVarInt(offset);
            if (bytes <= 0 || value <= 0) return false;

            id = value;
            offset += bytes;
            return HasRemainingBytes();
        }

        /// <summary>Reads a varint whose value is not validated beyond being present.</summary>
        public bool ReadVarInt(out int value)
        {
            value = -1;
            var (v, bytes) = data.ReadVarInt(offset);
            if (bytes <= 0) return false;

            offset += bytes;
            value = v;
            return HasRemainingBytes();
        }

        /// <summary>Reads a single byte and advances the offset.</summary>
        public bool ReadByte(out byte value)
        {
            value = 0;
            if (!HasRemainingBytes()) return false;
            value = data[offset++];
            return true;
        }

        public bool HasRemainingBytes(int count = 1) => offset + count <= data.Length;

        public byte[] Data => data;
    }
}
