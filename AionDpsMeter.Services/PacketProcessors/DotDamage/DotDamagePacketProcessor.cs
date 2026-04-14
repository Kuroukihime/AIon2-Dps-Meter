using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors.DotDamage
{
    internal sealed class DotDamagePacketProcessor
    {
        public event EventHandler<PlayerDamage>? DamageReceived;

        private readonly GameDataProvider gameData;
        private readonly EntityTracker entityTracker;
        private readonly ILogger<DotDamagePacketProcessor> logger;

        public DotDamagePacketProcessor(EntityTracker entityTracker, ILogger<DotDamagePacketProcessor> logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
        }

        public void Process(byte[] packet)
        {
            var reader = new DotDamagePacketReader(packet);
            if (!reader.ReadAndValidateHeader()) return;
            if (!reader.ReadTargetId(out int targetId)) return;
            if (!reader.ReadAndValidateEffectType(out _)) return;
            if (!reader.ReadActorId(out int actorId)) return;
            if (!reader.ReadUnknownVarInt(out _)) return;
            if (!reader.ReadSkillCode(out int skillCode)) return;
            if (!reader.ReadDamage(out long damage)) return;

            if (targetId == actorId) return;

            CharacterClass? characterClass;
            if (!gameData.IsTheostone(skillCode))
            {
                characterClass = gameData.GetClassBySkillCode(skillCode);
                if (characterClass == null)
                {
                    logger.LogWarning("Unknown class for skill code: {SkillCode}", skillCode);
                    return;
                }
                if (!gameData.IsDotDamageSkill(skillCode)) return;
                if (gameData.IsHealingSkill(skillCode)) return;
            }
            else
            {
                var player = entityTracker.GetPlayerEntity(actorId);
                if (player == null)
                {
                    logger.LogWarning("Unknown player for theostone code: {SkillCode}", skillCode);
                    return;
                }
                characterClass = player.CharacterClass;
            }

            DamageReceived?.Invoke(this, new PlayerDamage
            {
                DateTime = DateTime.Now,
                SourceEntity = entityTracker.GetOrCreatePlayerEntity(actorId, characterClass),
                TargetEntity = entityTracker.GetOrCreateTargetEntity(targetId),
                Skill = gameData.GetSkillOrDefault(skillCode),
                CharacterClass = characterClass,
                Damage = damage,
                IsDot = true,
            });
        }
    }
}
