using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AionDpsMeter.Services.PacketProcessors.Nickname
{
    internal sealed class NicknamePacketProcessor
    {
        private readonly EntityTracker entityTracker;
        private readonly ILogger<NicknamePacketProcessor> logger;

        private const int NameScanWindow = 10;
        private const byte NameBlockMarker = 7;
        private const int MaxNameLength = 72;

        private static readonly byte[] InfoTag1 = [0x33, 0x36];
        private static readonly byte[] InfoTag2 = [0x44, 0x36];

        public NicknamePacketProcessor(EntityTracker entityTracker, ILogger<NicknamePacketProcessor> logger)
        {
            this.entityTracker = entityTracker;
            this.logger = logger;
        }

        private readonly record struct PlayerInfoResult(int EntityId, string Name, int ServerId, int JobCode);
        private readonly record struct NameReadResult(string Name, int EndOffset);

        public void ProcessPlayerInfo(byte[] packet)
        {
            if (!TryParseInfoTag1(packet, packet.Length, out PlayerInfoResult result)) return;
            entityTracker.UpdatePlayerEntityName(result.EntityId, result.Name, ServerMap.GetName(result.ServerId), true);
        }

        public void ProcessOtherPlayersInfo(byte[] packet)
        {
            if (!TryParseInfoTag2(packet, packet.Length, out PlayerInfoResult result2)) return;
            entityTracker.UpdatePlayerEntityName(result2.EntityId, result2.Name, ServerMap.GetName(result2.ServerId));
        }

        public void ProcessGlobalSessionIdLinking(byte[] packet)
        {
            if (packet.Length < 4) return;
            var lenVarInt = packet.ReadVarInt().Length;
            ProcessIdLinkinPacket(packet, lenVarInt);
        }

        public void ProcessPartyPacket(byte[] packet)
        {
            if (packet.Length < 4) return;
            var lenVarInt = packet.ReadVarInt().Length;
            ProcessPartyPacket(packet, lenVarInt);
        }

        private void ProcessIdLinkinPacket(byte[] packet, int lenVarInt)
        {
            var playerIdVarInt = packet.ReadVarInt(lenVarInt + 4);
            var playerId = playerIdVarInt.Value;
            var globalId = packet.ReadUInt32Le(lenVarInt + playerIdVarInt.Length + 8);

            var linkResult = entityTracker.LinkBaseToSessionPlayerEntity(globalId, playerId);
            logger.LogInformation("Player globalId {GlobalId} => sessionId {PlayerId}. Link result {LinkResult}", globalId, playerId, linkResult);
        }

        private void ProcessPartyPacket(byte[] packet, int lenVarInt)
        {
            List<Player> list = ParsePartyMemberBlocksStructured(packet, lenVarInt + 2);
            if (list.Count > 0)
            {
                Trace.WriteLine(BitConverter.ToString(packet));
                foreach (var partyMember in list)
                    entityTracker.RegisterBasePlayerEntity(partyMember);
            }
        }

        private List<Player> ParsePartyMemberBlocksStructured(byte[] packet, int dataOffset)
        {
            var results = new List<Player>();
            byte[] levelMarker = [0x2D, 0x00, 0x00, 0x00];
            int searchFrom = dataOffset;
            int expectedMemberNum = 1;

            while (true)
            {
                int levelPos = packet.IndexOfArray(levelMarker, searchFrom);
                if (levelPos < 0) break;

                int blockStart = -1;
                int foundNickLen = -1;
                for (int nickLen = 1; nickLen <= 48; nickLen++)
                {
                    int bs = levelPos - 14 - nickLen;
                    if (bs < dataOffset || bs + 24 + nickLen > packet.Length) continue;
                    if (packet[bs + 9] != nickLen) continue;

                    int s1 = packet[bs + 7] | (packet[bs + 8] << 8);
                    if (!ServerMap.IsValidId(s1)) continue;

                    int server2Pos = levelPos + 8;
                    if (server2Pos + 2 > packet.Length) continue;
                    int s2 = packet[server2Pos] | (packet[server2Pos + 1] << 8);
                    if (!ServerMap.IsValidId(s2)) continue;
                    if (packet[bs] != expectedMemberNum) continue;

                    blockStart = bs;
                    foundNickLen = nickLen;
                    break;
                }

                if (blockStart < 0)
                {
                    if (results.Count > 0) break;
                    searchFrom = levelPos + 1;
                    continue;
                }

                int id = packet.ReadUInt32Le(blockStart + 1);
                if (id == 0) break;

                string nickname;
                try { nickname = DecodeGameString(packet, blockStart + 10, foundNickLen); }
                catch { break; }

                int server1 = packet[blockStart + 7] | (packet[blockStart + 8] << 8);
                int level = packet.ReadUInt32Le(blockStart + 14 + foundNickLen);
                int combatPower = packet.ReadUInt32Le(blockStart + 18 + foundNickLen);

                results.Add(new Player
                {
                    Id = id,
                    ServerId = server1,
                    ServerName = ServerMap.GetName(server1),
                    Name = nickname,
                    CharactedLevel = level,
                    CombatPower = combatPower
                });

                expectedMemberNum++;
                searchFrom = levelPos + 4;
            }

            return results;
        }

        private bool TryParseInfoTag1(byte[] data, int endOffset, out PlayerInfoResult result)
        {
            result = default;
            int tagPos = data.IndexOfArray(InfoTag1);
            if (tagPos < 0) return false;

            int pos = tagPos + 2;
            if (pos >= endOffset) return false;

            var entityVarInt = data.ReadVarInt(pos);
            int entityId = entityVarInt.Value;
            if (entityId < 1) return false;
            pos += entityVarInt.Length;

            var nameRead = ReadPlayerName(data, pos, endOffset);
            if (!nameRead.HasValue) return false;

            int afterNameOffset = nameRead.Value.EndOffset;
            int serverId = afterNameOffset + 2 <= endOffset ? data[afterNameOffset] | (data[afterNameOffset + 1] << 8) : -1;
            int jobCode = afterNameOffset + 3 <= endOffset ? data[afterNameOffset + 2] : -1;

            result = new PlayerInfoResult(entityId, nameRead.Value.Name, serverId, jobCode);
            return true;
        }

        private bool TryParseInfoTag2(byte[] data, int endOffset, out PlayerInfoResult result)
        {
            result = default;
            int tagPos = data.IndexOfArray(InfoTag2);
            if (tagPos < 0) return false;

            int pos = tagPos + 2;
            if (pos >= endOffset) return false;

            var entityVarInt = data.ReadVarInt(pos);
            int entityId = entityVarInt.Value;
            if (entityId < 1) return false;
            pos += entityVarInt.Length;

            if (pos < endOffset) pos += data.ReadVarInt(pos).Length;
            if (pos < endOffset) pos += data.ReadVarInt(pos).Length;

            var nameRead = ReadPlayerName(data, pos, endOffset);
            if (!nameRead.HasValue) return false;

            int afterNameOffset = nameRead.Value.EndOffset;
            int jobCode = -1;
            var jobCodeVarInt = data.ReadVarInt(afterNameOffset);
            if (jobCodeVarInt.Value < 1) jobCode = jobCodeVarInt.Value;
            else afterNameOffset += jobCodeVarInt.Length;

            int serverId = FindServerId(data, afterNameOffset, endOffset, ServerMap.Servers.Keys.ToHashSet());
            result = new PlayerInfoResult(entityId, nameRead.Value.Name, serverId, jobCode);
            return true;
        }

        private NameReadResult? ReadPlayerName(byte[] data, int searchStart, int endOffset)
        {
            int scanEnd = Math.Min(searchStart + NameScanWindow, endOffset);
            for (int i = searchStart; i < scanEnd; i++)
            {
                if (data[i] != NameBlockMarker) continue;

                int pos = i + 1;
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

        private static int FindServerId(byte[] data, int searchStart, int endOffset, HashSet<int>? validServerIds)
        {
            for (int i = searchStart; i < endOffset - 1; i++)
            {
                int candidateId = data[i] | (data[i + 1] << 8);
                if (validServerIds != null && validServerIds.Contains(candidateId))
                    return candidateId;
            }
            return -1;
        }

        private static string DecodeGameString(byte[] data, int offset, int maxLen)
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
