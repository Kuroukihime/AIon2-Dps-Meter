using AionDpsMeter.Services.Models;
using AionDpsMeter.UI.Utils;
using System.Globalization;

namespace AionDpsMeter.Tests;

public sealed class CopyDpsFormatterTests
{
    [Fact]
    public void Format_UsesRawDamageAndInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            var result = CopyDpsFormatter.Format(
            [
                Player("Player1", 240678147),
                Player("Player2", 198450147),
                Player("Player3", 15245),
            ]);

            Assert.Equal(
                "Player1 : 240,678,147 dmg, Player2 : 198,450,147 dmg, Player3 : 15,245 dmg",
                result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData(999, "A : 999 dmg")]
    [InlineData(1000, "A : 1,000 dmg")]
    [InlineData(15245, "A : 15,245 dmg")]
    [InlineData(240678147, "A : 240,678,147 dmg")]
    public void Format_UsesCommaThousandsSeparators(long damage, string expected)
    {
        Assert.Equal(expected, CopyDpsFormatter.Format([Player("A", damage)]));
    }

    [Fact]
    public void Format_SortsByNumericDamageAndKeepsOnlyTenPlayers()
    {
        var players = new[]
        {
            Player("Player4", 40),
            Player("Player1", 100),
            Player("Player12", 1),
            Player("Player3", 60),
            Player("Player6", 20),
            Player("Player2", 80),
            Player("Player5", 30),
            Player("Player7", 10),
            Player("Player8", 5),
            Player("Player9", 15),
            Player("Player10", 25),
            Player("Player11", 2),
        };

        var result = CopyDpsFormatter.Format(players);

        Assert.Equal(
            "Player1 : 100 dmg, Player2 : 80 dmg, Player3 : 60 dmg, Player4 : 40 dmg, Player5 : 30 dmg, Player10 : 25 dmg, Player6 : 20 dmg, Player9 : 15 dmg, Player7 : 10 dmg, Player8 : 5 dmg",
            result);
        Assert.DoesNotContain("Player11", result);
        Assert.DoesNotContain("Player12", result);
    }

    [Fact]
    public void Format_HandlesFewerThanTenPlayersAndOnePlayer()
    {
        var players = new[] { Player("Player2", 1000), Player("Player1", 999) };

        Assert.Equal("Player2 : 1,000 dmg, Player1 : 999 dmg", CopyDpsFormatter.Format(players));
        Assert.Equal("Only : 999 dmg", CopyDpsFormatter.Format([Player("Only", 999)]));
    }

    [Fact]
    public void Format_ReturnsEmptyTextWhenThereAreNoPlayers()
    {
        Assert.Equal(string.Empty, CopyDpsFormatter.Format([]));
    }

    [Fact]
    public void Format_PreservesInputOrderForEqualDamageAndDoesNotMutateValues()
    {
        var players = new[]
        {
            Player("First", 50),
            Player("Highest", 100),
            Player("Second", 50),
        };
        var damagesBefore = players.Select(player => player.TotalDamage).ToArray();

        var result = CopyDpsFormatter.Format(players);

        Assert.Equal("Highest : 100 dmg, First : 50 dmg, Second : 50 dmg", result);
        Assert.Equal(damagesBefore, players.Select(player => player.TotalDamage).ToArray());
    }

    [Fact]
    public void SessionOverviewUsesTheSameSnapshotFormatter()
    {
        var snapshot = new AionDpsMeter.Services.Services.Session.HistorySessionSnapshot
        {
            PlayerStats = [Player("Player1", 240678147), Player("Player2", 198450147), Player("Player3", 15245)],
        };
        var viewModel = new AionDpsMeter.UI.ViewModels.History.HistorySessionViewModel(
            snapshot,
            TestAppSettingsService.Create());

        Assert.Equal(
            "Player1 : 240,678,147 dmg, Player2 : 198,450,147 dmg, Player3 : 15,245 dmg",
            viewModel.CopyDpsText);
    }

    private static PlayerStats Player(string name, long damage) => new()
    {
        PlayerName = name,
        TotalDamage = damage,
        IsIdentified = true,
    };
}
