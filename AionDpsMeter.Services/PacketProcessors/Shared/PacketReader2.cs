using System;
using System.Collections.Generic;
using System.Text;

namespace AionDpsMeter.Services.PacketProcessors.Shared
{
    /// <summary>
    /// Bounds-checked little-endian binary cursor over a byte[]. Every read
    /// advances Position and throws PacketParseException instead of crashing
    /// or silently reading garbage if the packet is truncated/malformed.
    /// </summary>
    public sealed class PacketReader2
    {
        private readonly byte[] _data;
        public int Position { get; private set; }
        public int Remaining => _data.Length - Position;

        public PacketReader2(byte[] data, int offset = 0)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            Position = offset;
        }

        private void Require(int n)
        {
            if (Remaining < n)
                throw new PacketParseException($"Need {n} bytes but only {Remaining} remain", Position);
        }

        public void SetPosition(int newPosition)
        {
            if (newPosition < 0 || newPosition > _data.Length)
                throw new ArgumentOutOfRangeException(nameof(newPosition), "Position must be within the bounds of the data array.");
            Position = newPosition;
        }

        public byte ReadU8()
        {
            Require(1);
            return _data[Position++];
        }

        public byte PeekU8()
        {
            Require(1);
            return _data[Position];
        }

        public ushort ReadU16()
        {
            Require(2);
            ushort v = (ushort)(_data[Position] | (_data[Position + 1] << 8));
            Position += 2;
            return v;
        }


        public uint ReadU32()
        {
            Require(4);
            uint v = (uint)(_data[Position]
                     | (_data[Position + 1] << 8)
                     | (_data[Position + 2] << 16)
                     | (_data[Position + 3] << 24));
            Position += 4;
            return v;
        }

        public ulong ReadU64()
        {
            Require(8);
            ulong v = 0;
            for (int i = 0; i < 8; i++)
                v |= (ulong)_data[Position + i] << (8 * i);
            Position += 8;
            return v;
        }

        public uint ReadVarInt()
        {
            uint result = 0;
            int shift = 0;
            byte b;
            do
            {
                if (shift >= 35) throw new PacketParseException("Varint too long", Position);
                b = ReadU8();
                result |= (uint)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }

        /// <summary>[u8 len][UTF-8 bytes] string, as used for party_name / _nickname.</summary>
        public string ReadLengthPrefixedString()
        {
            byte len = ReadU8();
            Require(len);
            string s = Encoding.UTF8.GetString(_data, Position, len);
            Position += len;
            return s;
        }

        public byte[] ReadBytes(int count)
        {
            Require(count);
            var b = new byte[count];
            Array.Copy(_data, Position, b, 0, count);
            Position += count;
            return b;
        }

        public void Skip(int count) => ReadBytes(count);
    }


    public sealed class PacketParseException : Exception
    {
        public int Offset { get; }
        public PacketParseException(string message, int offset) : base($"{message} (offset {offset})")
        {
            Offset = offset;
        }
    }
}
