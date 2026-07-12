using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessors.Shared;
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


            var reader = new PacketReader(packet);

            reader.ReadVarInt(); //len
            reader.ReadU16();    //opcode

            var targetId = reader.ReadVarInt();

            var effectType = reader.ReadU8();
            if ((effectType & 0x02) == 0) return;

            var actorId = reader.ReadVarInt();
            reader.ReadVarInt(); // unknown

            var skillCode = ((int)reader.ReadU32()) / 100;
            if (!DataValidationHelper.IsReasonableSkillCode(skillCode)) return;

            var damage = reader.ReadVarInt();

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
                var player = entityTracker.GetPlayerEntity((int)actorId);
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
                SourceEntity = entityTracker.GetOrCreateSessionPlayer((int)actorId, characterClass),
                TargetEntity = entityTracker.GetOrCreateTargetEntity((int)targetId),
                Skill = gameData.GetSkillOrDefault(skillCode),
                CharacterClass = characterClass,
                Damage = damage,
                IsDot = true,
            });
        }
    }
}
