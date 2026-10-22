using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.MobSummon)]
    internal sealed class MobPacketProcessor(EntityTracker entityTracker) : IOpcodeProcessor
    {

        public void Process(Packet packet)
        {

            var r = new PacketReader(packet.Data);

            r.ReadVarInt(); //len
            r.ReadU16();    //opcode

            var entityId = r.ReadVarInt();
            var mask = r.ReadU16();

            var isPlayerSummon = (mask & 0x0010) != 0;

            ReadMobGeneralInfo(r, out var mobCode);
            ReadMobState(r, out _, out var maxHp );

            if (!isPlayerSummon)
            {
                entityTracker.CreateOrUpdateTargetEntity((int)entityId, (int)mobCode, (long)maxHp);
                return;
            }

            var entryCount = r.ReadVarInt();

            for (var i = 1; i <= entryCount; i++)
            {
               ReadMobEntry(r);
            }

            if ((mask & 0x0001) != 0) r.ReadU8();
            if ((mask & 0x0002) != 0) r.ReadU8();
            if ((mask & 0x0004) != 0) r.ReadU8();
            if ((mask & 0x0008) != 0) r.ReadBit();
            var ownerId = r.ReadU32(); //mask & 0x0010

            entityTracker.RegisterSummon((int)entityId, (int)ownerId);
        }


        private void ReadMobGeneralInfo(PacketReader r, out uint mobCode)
        {
            var idMask = r.ReadU8();
            if ((idMask & 0x01) != 0) r.ReadLengthPrefixedString(); //owner name (only for player summoned entities)

            mobCode = r.ReadU32();

            if ((idMask & 0x02) != 0) r.ReadU8();
            if ((idMask & 0x04) != 0) r.ReadU8();
            if ((idMask & 0x08) != 0) r.ReadU8();
            if ((idMask & 0x08) != 0) r.ReadU32();
            if ((idMask & 0x10) != 0) r.ReadU8();
        }

        private void ReadMobState(PacketReader r, out ulong hp, out ulong maxHp)
        {
            var stateMask = r.ReadU16();

            //position
            r.ReadU32(); //x
            r.ReadU32(); //y
            r.ReadU32(); //z
            r.ReadU32(); //heading

            r.ReadU16();

            if ((stateMask & 0x0001) != 0)
            {
                r.ReadU32();
                r.ReadU32();
                r.ReadU32();
            }

            if ((stateMask & 0x0002) != 0) r.ReadU32();
            if ((stateMask & 0x0004) != 0) r.ReadU16();

            if ((stateMask & 0x0008) != 0)
            {
                r.ReadU32();
                r.ReadU32();
                r.ReadU32();
            }

            if ((stateMask & 0x0010) != 0)
            {
                r.ReadU32();
                r.ReadU32();
                r.ReadU32();
            }

            if ((stateMask & 0x0020) != 0) r.ReadU32();

            r.ReadU8(); //isAlive
            hp = r.ReadVarInt64();
            maxHp = r.ReadVarInt64();

            //skip x13 u32
            r.Skip(52);

            if ((stateMask & 0x0040) != 0) r.ReadU8();
            if ((stateMask & 0x0080) != 0) r.ReadU32();
            if ((stateMask & 0x0100) != 0) r.ReadU8();
            if ((stateMask & 0x0200) != 0) r.ReadU8();
            if ((stateMask & 0x0400) != 0) r.ReadVarInt();
            if ((stateMask & 0x0800) != 0) r.ReadU32();
            if ((stateMask & 0x1090) != 0) r.ReadU32();
            if ((stateMask & 0x2000) != 0) r.ReadU8();
            if ((stateMask & 0x4000) != 0) r.ReadU32();
            if ((stateMask & 0x8000) != 0) r.ReadU32();
        }

        public void ReadMobEntry(PacketReader r)
        {
            var entryMask = r.ReadU8();
            r.ReadVarInt();
            r.ReadU32();
            r.ReadU64();
            r.ReadU64();
            if ((entryMask & 0x01) != 0) r.ReadVarInt();
            r.ReadU8();
            if ((entryMask & 0x02) != 0) r.ReadU32();
            r.ReadBit();
            if ((entryMask & 0x04) != 0) r.ReadVarInt();
            if ((entryMask & 0x08) != 0) r.ReadU32();
            if ((entryMask & 0x10) != 0)
            {
                r.ReadU32();
                r.ReadU32();
                r.ReadU32();
            }
        }

    }

}
