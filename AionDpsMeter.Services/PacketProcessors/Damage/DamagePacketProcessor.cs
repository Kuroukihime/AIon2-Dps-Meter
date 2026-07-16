using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
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

        public DamagePacketProcessor(EntityTracker entityTracker, ILogger<DamagePacketProcessor> logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
        }

        public void Process(byte[] packet)
        {
            var parsed = DamagePacketParser.Parse(packet);
            if (!parsed.IsValid)
            {
                logger.LogDebug("04-38 parsing failed: {Result}", parsed.Result);
                return;
            }

            var playerDamage = BuildPlayerDamage(parsed.Data!);
            if (playerDamage != null)
                DamageReceived?.Invoke(this, playerDamage);
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
    }
}
