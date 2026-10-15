using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Session.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AionDpsMeter.Tests;

public sealed class CombatHistoryStoreTests
{
    [Fact]
    public void SaveAndGetSession_PreservesPlayerDamageSnapshot()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"combat-history-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            DefaultTimeout = 10,
        }.ToString();
        using var keepAliveConnection = new SqliteConnection(connectionString);
        keepAliveConnection.Open();

        var options = new DbContextOptionsBuilder<CombatHistoryDbContext>()
            .UseSqlite(connectionString)
            .Options;
        var dbContextFactory = new TestDbContextFactory(options);
        var store = new CombatHistoryStore(
            dbContextFactory,
            NullLogger<CombatHistoryStore>.Instance,
            TestAppSettingsService.Create());

        var sessionId = Guid.NewGuid();
        var now = DateTime.Now;
        store.Save(new HistorySessionSnapshot
        {
            SessionId = sessionId,
            TargetId = 77,
            TargetName = "Training Dummy",
            SessionStart = now.AddMinutes(-1),
            SessionEnd = now,
            State = SessionState.Completed,
            PlayerStats =
            [
                new PlayerStats
                {
                    PlayerId = 1,
                    PlayerName = "Player1",
                    IsIdentified = true,
                    TotalDamage = 240678147,
                },
            ],
        });
        store.FlushPendingSaves();

        var snapshot = store.GetSession(sessionId);
        var player = Assert.Single(Assert.IsType<HistorySessionSnapshot>(snapshot).PlayerStats);
        var row = Assert.Single(store.GetSessionPage(null, null, "dummy", 0, 10));

        Assert.Equal("Player1", player.PlayerName);
        Assert.Equal(240678147, player.TotalDamage);
        Assert.Equal(240678147, row.TotalDamage);
        Assert.Equal(sessionId, row.SessionId);
    }

    private sealed class TestDbContextFactory(DbContextOptions<CombatHistoryDbContext> options)
        : IDbContextFactory<CombatHistoryDbContext>
    {
        public CombatHistoryDbContext CreateDbContext() => new(options);
    }
}
