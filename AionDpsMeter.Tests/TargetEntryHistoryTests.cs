using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;
using AionDpsMeter.Services.Services.Session;

namespace AionDpsMeter.Tests;

public sealed class TargetEntryHistoryTests
{
    [Fact]
    public void CompleteActiveSession_DoesNotPersistNonBossCombat()
    {
        const int targetId = 123;
        const int nonBossMobCode = 2_000_002;
        var start = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
        var target = new Mob { Id = targetId, MobCode = nonBossMobCode };
        Assert.False(GameDataProvider.Instance.IsBoss(nonBossMobCode));

        var completed = new List<TargetCombatSession>();
        var entry = new TargetEntry(
            targetId,
            new EntityTracker(),
            TestAppSettingsService.Create(),
            completed.Add);

        entry.AddDamage(new PlayerDamage
        {
            DateTime = start,
            Damage = 100,
            SourceEntity = new Player { Id = 42 },
            TargetEntity = target,
        });
        entry.CompleteActiveSession();

        Assert.Empty(completed);
        Assert.Null(entry.CurrentSession);
    }
}
