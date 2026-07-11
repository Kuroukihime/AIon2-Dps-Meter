using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AionDpsMeter.Services.PacketProcessors.PlayerEntity
{
    internal sealed class PlayerProcessor
    {
        private readonly EntityTracker entityTracker;
        private readonly ILogger<PlayerProcessor> logger;

        private const int MaxNameLength = 72;

        public PlayerProcessor(EntityTracker entityTracker, ILogger<PlayerProcessor> logger)
        {
            this.entityTracker = entityTracker;
            this.logger = logger;
        }

        private readonly record struct PlayerInfoResult(int EntityId, string Name, int ServerId, int JobCode);
        private readonly record struct NameReadResult(string Name, int EndOffset);

        public void ProcessPlayerInfo(byte[] packet)
        {
            if (!TryParseInfoTag1(packet, packet.Length, out PlayerInfoResult result)) return;
            entityTracker.SetSessionPlayerName(result.EntityId, result.Name, ServerMap.GetName(result.ServerId), true);
        }

        public void ProcessOtherPlayersInfo(byte[] packet)
        {
            if (!TryParseInfoTag2(packet, packet.Length, out PlayerInfoResult result2)) return;
            entityTracker.SetSessionPlayerName(result2.EntityId, result2.Name, ServerMap.GetName(result2.ServerId));
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

            entityTracker.LinkSessionToGlobalPlayer(globalId, playerId);
        }

        private void ProcessPartyPacket(byte[] packet, int lenVarInt)
        {
            List<Player> list = ParsePartyMemberBlocksStructured(packet, lenVarInt + 2);
            if (list.Count > 0)
            {           
                foreach (var partyMember in list)
                    entityTracker.RegisterOrUpdateGlobalPlayer(partyMember);
            }
        }

        private List<Player> ParsePartyMemberBlocksStructured(byte[] packet, int dataOffset)
        {

            var results = new List<Player>();

            try
            {

                //Debug.WriteLine(BitConverter.ToString(packet));
                var result = PartyPacketParser.Parse(packet);

                foreach (var partyMember in result.ValidMembers)
                {
                    results.Add(new Player
                    {
                        Id = (int)partyMember.Id,
                        ServerId = partyMember.ServerId,
                        ServerName = partyMember.ServerName,
                        Name = partyMember.Name,
                        CharacterLevel = (int)partyMember.CharactedLevel,
                        CombatPower = (int)(partyMember.CombatPower ?? 0)
                    });
                }
            }
            catch (Exception e) { 
                Debug.WriteLine(BitConverter.ToString(packet));
            }

            return results;
        }

        private bool TryParseInfoTag1(byte[] data, int endOffset, out PlayerInfoResult result)
        {
            result = default;
            var pos = data.ReadVarInt().Length + 2;    
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
            var pos = data.ReadVarInt().Length + 2;
            if (pos >= endOffset) return false;

            var entityVarInt = data.ReadVarInt(pos);
            int entityId = entityVarInt.Value;
            if (entityId < 1) return false;
            pos += entityVarInt.Length;
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

        private NameReadResult? ReadPlayerName(byte[] data, int start, int endOffset)
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
