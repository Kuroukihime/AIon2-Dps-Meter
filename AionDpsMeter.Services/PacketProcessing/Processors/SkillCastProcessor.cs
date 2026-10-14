using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Session;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.SkillCast)]
    internal class SkillCastProcessor(CombatSessionManager sessionManager) : IOpcodeProcessor
    {
        private const byte MaskHas01 = 0x01;
        private const byte MaskHasUnknown02 = 0x02;
        private const byte MaskHasAutoloadCount = 0x04;
        private const byte MaskHasAutoloadTime= 0x08;
        private const byte MaskHasUnknown10 = 0x10;
        private const byte MaskHasCount = 0x20;

        private readonly GameDataProvider gameData = GameDataProvider.Instance;
        public void Process(Packet packet)
        {
            var r = new PacketReader(packet.Data);

            r.ReadVarInt(); //len
            r.ReadU16(); //opcode

            var sourceId = r.ReadVarInt();
            var mask = r.ReadU8();
            var skillId = r.ReadU32();
            r.ReadU8(); //uid1  

            if ((mask & MaskHas01) != 0) r.ReadU8();
            ReadTargetInfo(r);

            r.ReadVarInt(); //unknown
            r.ReadU8(); //unknown

            if ((mask & MaskHasUnknown02) != 0) r.ReadVarInt(); //unknown
            var cooldownMilliseconds = r.ReadVarInt();
            if(cooldownMilliseconds <1) return;

            if ((mask & MaskHasAutoloadCount) != 0) r.ReadU8(); //autoloadCount
            if ((mask & MaskHasAutoloadTime) != 0) r.ReadVarInt(); //autoloadTime
            if ((mask & MaskHasUnknown10) != 0) r.ReadBit(); //unknown bit
            if ((mask & MaskHasCount) != 0) r.ReadVarInt(); //count

            var skill = gameData.GetSkillOrDefault((int)skillId);

            sessionManager.RegisterSkillCastEvent(new SkillCastEvent()
            {
                Name = skill.Name,
                IconUrl = skill.Icon,
                SkillId = skill.Id,
                EntityId = (int)sourceId,
                Cd = TimeSpan.FromMilliseconds(cooldownMilliseconds)

            });


        }

        private void ReadTargetInfo(PacketReader r)
        {
            var mask = r.ReadU8();
            r.ReadVarInt(); // key
            if ((mask & 0x01) != 0) r.ReadU32();
            r.Skip(4); //yaw 
            if ((mask & 0x02) != 0) r.Skip(12); //location 
        }

    }
}
