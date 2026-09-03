using AionDpsMeter.Services.Extensions;
using System.Text;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    internal class BasePlayerInfoProcessor
    {
        private const int MaxNameLength = 72;
        private const ulong MinCombatPower = 10_000;
        private const ulong MaxCombatPower = 2_000_000;
        private const int CombatPowerScanWindow = 256;
        protected readonly record struct PlayerInfoResult(int EntityId, string Name, int ServerId, int JobCode, int CombatPower);
        protected readonly record struct NameReadResult(string Name, int EndOffset);

        protected static bool TryParseCombatPower(byte[] data, int endOffset, out int combatPower)
        {
            combatPower = 0;
            const int pairSize = sizeof(ulong) * 2;
            if (endOffset < pairSize) return false;

            int startOffset = endOffset - pairSize;
            int minOffset = Math.Max(0, startOffset - (CombatPowerScanWindow - 1));

            for (int offset = startOffset; offset >= minOffset; offset--)
            {
                ulong currentCombatPower = data.ReadUInt64Le(offset);
                ulong highestCombatPower = data.ReadUInt64Le(offset + sizeof(ulong));

                bool isCurrentInRange = currentCombatPower >= MinCombatPower && currentCombatPower <= MaxCombatPower;
                bool isHighestInRange = highestCombatPower >= MinCombatPower && highestCombatPower <= MaxCombatPower;

                if (!isCurrentInRange || !isHighestInRange) continue;
                if (currentCombatPower > highestCombatPower) continue;

                combatPower = (int)currentCombatPower;

                return true;
            }

            return false;
        }

        protected NameReadResult? ReadPlayerName(byte[] data, int start, int endOffset)
        {

            start += 4;
            if ((data[start] & 0x01) != 0)
            {
                int pos = start + 1;
                var nameLengthVarInt = data.ReadVarInt(pos);
                int nameByteLength = nameLengthVarInt.Value;
                if (nameByteLength < 1 || nameByteLength > MaxNameLength) return null;
                pos += nameLengthVarInt.Length;
                if (pos + nameByteLength > endOffset) return null;
                string name = DecodeGameString(data, pos, nameByteLength);
                if (string.IsNullOrEmpty(name) || IsAllDigits(name)) return null;

                return new NameReadResult(name, pos + nameByteLength);
            }
            return null;
        }

        protected static int FindServerId(byte[] data, int searchStart, int endOffset, HashSet<int>? validServerIds)
        {
            for (int i = searchStart; i < endOffset - 1; i++)
            {
                int candidateId = data[i] | (data[i + 1] << 8);
                if (validServerIds != null && validServerIds.Contains(candidateId))
                    return candidateId;
            }
            return -1;
        }

        protected static string DecodeGameString(byte[] data, int offset, int maxLen)
        {
            byte[] outputBuffer = new byte[maxLen * 4];
            int writePos = 0;
            int readEnd = offset + maxLen;

            for (int i = offset; i < readEnd; i++)
            {
                byte b = data[i];
                if (b == 0) break;
                if (b < 32)
                {
                    int repeatCount = Math.Min(b, writePos);
                    for (int j = 0; j < repeatCount && writePos < outputBuffer.Length; j++)
                        outputBuffer[writePos++] = outputBuffer[j];
                }
                else if (writePos < outputBuffer.Length)
                {
                    outputBuffer[writePos++] = b;
                }
            }

            string rawString = Encoding.UTF8.GetString(outputBuffer, 0, writePos);
            var cleanName = new StringBuilder(rawString.Length);
            foreach (char c in rawString)
                if (char.IsLetterOrDigit(c) || (c >= '?' && c <= '?'))
                    cleanName.Append(c);

            return cleanName.ToString();
        }

        private static bool IsAllDigits(string value)
        {
            foreach (char c in value)
                if (!char.IsDigit(c)) return false;
            return true;
        }
    }
}
