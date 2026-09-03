using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.PacketProcessing.Shared.Exceptions;
using AionDpsMeter.Services.Services.Entity;
using AionDpsMeter.Services.Services.Session;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.Damage)]
    public sealed class DamagePacketProcessor : IOpcodeProcessor
    {
        private const int CriticalDamageType = 3;
        private readonly GameDataProvider gameData;
        private readonly EntityTracker entityTracker;
        private readonly CombatSessionManager sessionManager;
        private readonly ILogger<DamagePacketProcessor> logger;

        public DamagePacketProcessor(EntityTracker entityTracker, CombatSessionManager sessionManager, ILogger<DamagePacketProcessor> logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
            this.sessionManager = sessionManager;
        }

      
        public void Process(Packet packet)
        {
            var parsed = Parse(packet.Data);
            if (!parsed.IsValid)
            {
                logger.LogDebug("04-38 parsing failed: {Result}", parsed.Result);
                return;
            }

            var playerDamage = BuildPlayerDamage(parsed.Data!);
            if (playerDamage != null)
                sessionManager.ProcessDamageEvent(playerDamage);
        }

        private PlayerDamage? BuildPlayerDamage(DamagePacketData data)
        {
            if (gameData.IsHealingSkill(data.SkillCode)) return null;
            CharacterClass? characterClass;
            if (!gameData.IsTheostone(data.SkillCode))
            {
                characterClass = gameData.GetClassBySkillCode(data.SkillCode);
                if (characterClass == null)
                {
                    logger.LogWarning("Unknown class for skill code: {SkillCode}", data.SkillCode);
                    return null;
                }
            }
            else
            {
                var player = entityTracker.GetPlayerEntity(data.ActorId);
                if (player == null)
                {
                    logger.LogWarning("Unknown player for theostone code: {SkillCode}", data.SkillCode);
                    return null;
                }
                characterClass = player.CharacterClass;
            }

            return new PlayerDamage
            {
                DateTime = data.Timestamp,
                SourceEntity = entityTracker.GetOrCreateSessionPlayer(data.ActorId, characterClass!),
                TargetEntity = entityTracker.GetOrCreateTargetEntity(data.TargetId),
                Skill = gameData.GetSkillOrDefault(data.SkillCode),
                CharacterClass = characterClass!,
                Damage = data.Damage,
                IsCritical = data.IsCritical,
                IsBackAttack = data.IsBackAttack,
                IsPerfect = data.IsPerfect,
                IsDoubleDamage = data.IsDoubleDamage,
                IsParry = data.IsParry,
                IsFrontAttack = data.IsFrontAttack
            };
        }

        private ParsedDamagePacket Parse(byte[] packet)
        {

            var reader = new PacketReader(packet);

            reader.ReadVarInt(); //len
            reader.ReadU16();    //opcode

            var targetId = reader.ReadVarInt();
            var switchValue = ProcessSwitchValue((int)reader.ReadVarInt());
            if (switchValue < 0) return Fail(PacketProcessResult.SWITH_VALUE_ERROR);
            reader.ReadVarInt(); // unkown flag
            var actorId = reader.ReadVarInt();

            if (actorId == targetId) return Fail(PacketProcessResult.ACTORID_EQUALS_TARGETID);

            var skillCode = reader.ReadU32();
            if (!DataValidationHelper.IsReasonableSkillCode((int)skillCode)) return Fail(PacketProcessResult.SKILLCODE_ERROR);
            reader.ReadU8(); // unknown
            var damageType = reader.ReadVarInt();
            var specialFlags = ReadSpecialBytesBlock(reader, switchValue);
            var unknownVarInt = reader.ReadVarInt(); // unknown
            var damage = reader.ReadVarInt();

            var data = new DamagePacketData
            {
                TargetId = (int)targetId,
                ActorId = (int)actorId,
                SkillCode = (int)skillCode,
                DamageType = (int)damageType,
                Damage = damage,
                IsCritical = damageType == CriticalDamageType,
                IsBackAttack = specialFlags.IsBackAttack,
                IsFrontAttack = specialFlags.IsFrontAttack,
                IsParry = specialFlags.IsParry,
                IsPerfect = specialFlags.IsPerfect,
                IsDoubleDamage = specialFlags.IsDoubleDamage,
                UnknownVarInt = (int)unknownVarInt
            };

            return new ParsedDamagePacket(data, PacketProcessResult.SUCCES);

        }

        private int ProcessSwitchValue(int switchVar)
        {
            if (switchVar > 255) return -1;
            int switchVal = switchVar & 0x0F;
            if (switchVal == 0 || (uint)(switchVal - 4) > 3) return -1;
            return switchVal;
        }

        private SpecialFlags ReadSpecialBytesBlock(PacketReader reader, int switchValue)
        {
            SpecialFlags flags = default;
            byte attackDirectionType = 0;
            byte damageFlagByte = 0;
            if (switchValue == 4)
            {
                if (reader.Remaining < 8) throw new PacketProcessException("Not enough bytes remaining for special flags");
            }
            else
            {
                if (reader.Remaining < 12) throw new PacketProcessException("Not enough bytes remaining for special flags");
                damageFlagByte = reader.ReadU8();
                reader.ReadU8(); // unknown
                attackDirectionType = reader.ReadU8();
            }

            flags = ParseSpecialFlags(damageFlagByte);

            flags.IsBackAttack = (attackDirectionType & 0x01) != 0;
            flags.IsFrontAttack = (attackDirectionType & 0x02) != 0;

            reader.ReadU32(); // unknown

            reader.Skip(4); //unknown tail bytes

            return flags;

        }

        private static ParsedDamagePacket Fail(PacketProcessResult result) => new(null, result);
        private record ParsedDamagePacket(DamagePacketData? Data, PacketProcessResult Result)
        {
            public bool IsValid => Result == PacketProcessResult.SUCCES;
        }

        private struct SpecialFlags
        {
            public bool IsBackAttack { get; set; }
            public bool IsFrontAttack { get; set; }
            public bool IsParry { get; init; }
            public bool IsPerfect { get; init; }
            public bool IsDoubleDamage { get; init; }
        }

        private static SpecialFlags ParseSpecialFlags(byte flagByte) => new()
        {
            IsBackAttack = (flagByte & 0x01) != 0,
            IsParry = (flagByte & 0x02) != 0,
            IsPerfect = (flagByte & 0x04) != 0,
            IsDoubleDamage = (flagByte & 0x08) != 0
        };


        private  class DamagePacketData
        {
            public int TargetId { get; init; }
            public int ActorId { get; init; }
            public int SkillCode { get; init; }
            public int DamageType { get; init; }
            public long Damage { get; init; }
            public bool IsCritical { get; init; }
            public bool IsBackAttack { get; init; }
            public bool IsFrontAttack { get; init; }
            public bool IsParry { get; init; }
            public bool IsPerfect { get; init; }
            public bool IsDoubleDamage { get; init; }
            public DateTime Timestamp { get; init; } = DateTime.Now;
            public int UnknownVarInt { get; init; }
        }
    }
}
