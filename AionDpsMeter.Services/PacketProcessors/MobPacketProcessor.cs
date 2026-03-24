using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class MobPacketProcessor (EntityTracker entityTracker, ILogger<MobPacketProcessor> logger )
    {

        public void ProcessMobHp(byte[] data)
        {
            int offset = 3;
            var mobIdInf = data.ReadVarInt(offset);
            offset += mobIdInf.Length;
            var mobId = mobIdInf.Value;

            offset += data.ReadVarInt(offset).Length;
            offset += data.ReadVarInt(offset).Length;
            offset += data.ReadVarInt(offset).Length;

            var hpInf = data.ReadUInt32Le(offset);
            entityTracker.UpdateTargetEntityHpCurrent(mobId, hpInf);
        }

        public bool ParseSummonSpawnAt(byte[] packet)
        {
            var headerLen = packet.ReadVarInt().Length + 2;
            var offset = headerLen;
            var targetInfo = packet.ReadVarInt(offset);
            if (targetInfo.Length < 0) return false;
            offset += targetInfo.Length;

            // Strip Object Type
            long realActorId = targetInfo.Value;
            if (realActorId > 1_000_000)
            {
                realActorId = (realActorId & 0x3FFF) | 0x4000;
            }
            int mobTypeId = -1;
            int maxHp = 0;

            bool foundSomething = false;


            //int codeMarkerIdx = packet.IndexOfArray([0x00, 0x40, 0x02]);
            //if (codeMarkerIdx == -1)
            //{
            //    codeMarkerIdx = packet.IndexOfArray([0x00, 0x00, 0x02]);
            //}

         

            //if (codeMarkerIdx != -1)
            //{
            //    int mobCode = (packet[codeMarkerIdx - 1] << 16) |
            //                  (packet[codeMarkerIdx - 2] << 8) |
            //                  (packet[codeMarkerIdx - 3]);

            //    //DataManager.SaveMobId(summonInfo.Value, mobCode);
            //    if (mobCode == 2310509)
            //    {
            //        Console.WriteLine();
            //    }

            //}

            // Scan for NPC Type
            int scanOffset = offset;
            int maxScan = Math.Min(packet.Length - 2, offset + 60);

            while (scanOffset < maxScan)
            {
                if (packet[scanOffset] == 0x00 &&
                    (packet[scanOffset + 1] == 0x40 || packet[scanOffset + 1] == 0x00) &&
                    packet[scanOffset + 2] == 0x02)
                {
                    if (scanOffset - 3 >= offset)
                    {
                        int b1 = packet[scanOffset - 3];
                        int b2 = packet[scanOffset - 2];
                        int b3 = packet[scanOffset - 1];
                        mobTypeId = b1 | (b2 << 8) | (b3 << 16);
                    }
                    break;
                }
                scanOffset++;
            }



            if (mobTypeId != -1)
            {

                //dataStorage.AppendMob(realActorId, mobTypeId);

                int hpScanOffset = scanOffset + 3;
                int hpScanEnd = Math.Min(packet.Length - 2, hpScanOffset + 64);

                while (hpScanOffset < hpScanEnd)
                {
                    if (packet[hpScanOffset] == 0x01)
                    {
                        var currentHpInfo = packet.ReadVarInt(hpScanOffset + 1);
                        if (currentHpInfo.Length > 0 && currentHpInfo.Value > 0)
                        {
                            var maxHpInfo = packet.ReadVarInt(hpScanOffset + 1 + currentHpInfo.Length);
                            if (maxHpInfo.Length > 0 && maxHpInfo.Value >= currentHpInfo.Value)
                            {
                                maxHp = maxHpInfo.Value;
                                //if (maxHpInfo.Value > 1000000)
                                //{
                                //    Console.WriteLine();

                                //}
                                //dataStorage.AppendMobHp(realActorId, (int)maxHpInfo.Value);
                                //logger.Debug("Summon mob HP mapped: Target {0} -> Max HP {1}", realActorId, maxHpInfo.Value);
                                break;
                            }
                        }
                    }
                    hpScanOffset++;
                }
                foundSomething = true;
            }
            entityTracker.CreateOrUpdateTargetEntity(targetInfo.Value, mobTypeId, maxHp);
            return foundSomething;
        }
    }
}
