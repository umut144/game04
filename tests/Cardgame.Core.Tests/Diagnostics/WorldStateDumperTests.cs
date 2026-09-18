namespace Cardgame.Core.Tests.Diagnostics;

using Cardgame.Core.Commands;
using Cardgame.Core.Diagnostics;
using Cardgame.Core.Model;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

public sealed class WorldStateDumperTests
{
    [Fact]
    public void DumpMentionsTheSeedAndEachPlayersZoneCounts()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var deck = TestCardDesigns.BuildDeck(8);
        var command = new SetupMatchCommand
        {
            Seed = 2026,
            MatchMode = MatchMode.PerfectMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };
        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        string dump = WorldStateDumper.Dump(world);

        Assert.Contains("seed=2026", dump);
        string starter = world.Turn.StartingPlayer.ToString();
        string other = PlayerIds.Opponent(world.Turn.StartingPlayer).ToString();
        Assert.Contains($"round=1 active={starter}", dump);
        Assert.Contains($"{starter}: deck=4 hand=4 destroyed=0 mana=10/10 debt=0 life=14/14 time=34s coins=0", dump);
        Assert.Contains($"{other}: deck=5 hand=3 destroyed=0 mana=10/10 debt=0 life=14/14 time=34s coins=0", dump);
        Assert.Contains("PlayerA board: totems=A:", dump);
        Assert.Contains("slots=[-,-,-,-,-,-]", dump);
    }
}
