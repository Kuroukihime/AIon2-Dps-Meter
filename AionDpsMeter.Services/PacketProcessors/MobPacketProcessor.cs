using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class MobPacketProcessor (EntityTracker entityTracker, ILogger<MobPacketProcessor> logger )
    {
        private const int NpcTypeScanWindow = 60;
        private const int HpScanWindow = 64;

        /// <summary>
        /// Processes a mob HP update packet and updates the entity tracker with the current HP value.
        /// </summary>
        public void ProcessMobHp(byte[] data)
        {
            int offset = 3;

            var mobIdInfo = data.ReadVarInt(offset);
            offset += mobIdInfo.Length;
            var mobId = mobIdInfo.Value;

            offset += data.ReadVarInt(offset).Length;
            offset += data.ReadVarInt(offset).Length;
            offset += data.ReadVarInt(offset).Length;

            var hpCurrent = data.ReadUInt32Le(offset);
            entityTracker.UpdateTargetEntityHpCurrent(mobId, hpCurrent);
        }

        /// <summary>
        /// Parses a summon/spawn packet and registers or updates the entity in the tracker.
        /// </summary>
        public void ProcessMobSpawn(byte[] packet)
        {
            int offset = packet.ReadVarInt().Length + 2;

            var targetInfo = packet.ReadVarInt(offset);
            if (targetInfo.Length <= 0) return;
            offset += targetInfo.Length;

            int mobTypeId = -1;
            int maxHp = 0;

            if (TryScanNpcTypeId(packet, offset, out int markerOffset))
            {
                mobTypeId = packet.ReadUInt32Le(markerOffset-3); 
                maxHp = TryScanMaxHp(packet, markerOffset + 3);
            }

            entityTracker.CreateOrUpdateTargetEntity(targetInfo.Value, mobTypeId, maxHp);
        }

        /// <summary>
        /// Scans for the NPC type marker pattern and extracts the 3-byte mob type ID preceding it.
        /// Returns <c>true</c> if a valid marker was found; <paramref name="markerOffset"/> is set
        /// </summary>
        private static bool TryScanNpcTypeId(byte[] packet, int startOffset, out int markerOffset)
        {
            markerOffset = 0;
            int maxScan = Math.Min(packet.Length - 2, startOffset + NpcTypeScanWindow);

            for (int i = startOffset; i < maxScan; i++)
            {
                if (packet[i] != 0x00) continue;
                if (packet[i + 1] != 0x40 && packet[i + 1] != 0x00) continue;
                if (packet[i + 2] != 0x02) continue;
                if (i - 3 < startOffset) continue;

                markerOffset = i;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Scans forward from <paramref name="scanStart"/> for the HP block (marker byte 0x01)
        /// and returns the decoded max HP value, or 0 if not found.
        /// </summary>
        private static int TryScanMaxHp(byte[] packet, int scanStart)
        {
            int scanEnd = Math.Min(packet.Length - 2, scanStart + HpScanWindow);

            for (int i = scanStart; i < scanEnd; i++)
            {
                if (packet[i] != 0x01) continue;

                var currentHpInfo = packet.ReadVarInt(i + 1);
                if (currentHpInfo.Length <= 0 || currentHpInfo.Value <= 0) continue;

                var maxHpInfo = packet.ReadVarInt(i + 1 + currentHpInfo.Length);
                if (maxHpInfo.Length > 0 && maxHpInfo.Value >= currentHpInfo.Value)
                    return maxHpInfo.Value;
            }
            return 0;
        }
    }
}
