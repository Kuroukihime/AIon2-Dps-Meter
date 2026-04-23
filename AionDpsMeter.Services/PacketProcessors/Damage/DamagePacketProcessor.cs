using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors.Damage
{
    internal sealed class DamagePacketProcessor
    {
        public event EventHandler<PlayerDamage>? DamageReceived;

        private readonly GameDataProvider gameData;
        private readonly EntityTracker entityTracker;
        private readonly ILogger<DamagePacketProcessor> logger;
        private const int CriticalDamageType = 3;

        public DamagePacketProcessor(EntityTracker entityTracker, ILogger<DamagePacketProcessor> logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
        }

        public void Process(byte[] packet)
        {
            var parsed = ParseFullPacket(packet);
            if (!parsed.IsValid)
            {
                logger.LogDebug("04-38 parsing failed: {Result}", parsed.Result);
                return;
            }

            var playerDamage = BuildPlayerDamage(parsed.Data!);
            if (playerDamage != null)
                DamageReceived?.Invoke(this, playerDamage);
        }

        public ParsedDamagePacket ParseFullPacket(byte[] packet)
        {
            var reader = new DamagePacketReader(packet);

            if (!reader.ReadAndValidateHeader())
                return Fail(PacketProcessResult.HEADER_ERROR);

            if (!reader.ReadTargetId(out int targetId))
                return Fail(PacketProcessResult.TARGET_ID_ERROR);

            if (!reader.ReadAndValidateSwitchValue(out int switchValue))
                return Fail(PacketProcessResult.SWITH_VALUE_ERROR);

            if (!reader.SkipFlagField())
                return Fail(PacketProcessResult.FLAG_ERROR);

            if (!reader.ReadActorId(out int actorId))
                return Fail(PacketProcessResult.ACTOR_ID_ERROR);

            if (actorId == targetId)
                return Fail(PacketProcessResult.ACTORID_EQUALS_TARGETID);

            if (!reader.ReadSkillCode(out int skillCode))
                return Fail(PacketProcessResult.SKILLCODE_ERROR);

            if (!reader.ReadDamageType(out int damageType))
                return Fail(PacketProcessResult.DMG_TYPE_ERROR);

            if (!reader.ReadSpecialFlags(switchValue, out var specialFlags))
                return Fail(PacketProcessResult.SPECIAL_FLAGS_ERROR);

            if (!reader.ReadUnknownVarInt(out int unknownVarInt))
                return Fail(PacketProcessResult.UNKNOWN_FIELD_ERROR);

            if (!reader.ReadDamage(out long damage))
                return Fail(PacketProcessResult.DAMAGE_ERROR);

            var data = new DamagePacketData
            {
                TargetId = targetId,
                ActorId = actorId,
                SkillCode = skillCode,
                DamageType = damageType,
                Damage = damage,
                IsCritical = damageType == CriticalDamageType,
                IsBackAttack = specialFlags.IsBackAttack,
                IsParry = specialFlags.IsParry,
                IsPerfect = specialFlags.IsPerfect,
                IsDoubleDamage = specialFlags.IsDoubleDamage,
                UnknownVarInt = unknownVarInt
            };

            return new ParsedDamagePacket(data, PacketProcessResult.SUCCES);
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
                SourceEntity = entityTracker.GetOrCreatePlayerEntity(data.ActorId, characterClass!),
                TargetEntity = entityTracker.GetOrCreateTargetEntity(data.TargetId),
                Skill = gameData.GetSkillOrDefault(data.SkillCode),
                CharacterClass = characterClass!,
                Damage = data.Damage,
                IsCritical = data.IsCritical,
                IsBackAttack = data.IsBackAttack,
                IsPerfect = data.IsPerfect,
                IsDoubleDamage = data.IsDoubleDamage,
                IsParry = data.IsParry,
            };
        }

        private static ParsedDamagePacket Fail(PacketProcessResult result) => new(null, result);
    }
}
