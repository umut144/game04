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
            MirrorMode = MirrorMode.PerfectMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };
        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        string dump = WorldStateDumper.Dump(world);

        Assert.Contains("seed=2026", dump);
        Assert.Contains("PlayerA: deck=8 hand=0 destroyed=0", dump);
        Assert.Contains("PlayerB: deck=8 hand=0 destroyed=0", dump);
        Assert.Contains("PlayerA board: totems=A:", dump);
        Assert.Contains("slots=[-,-,-,-,-,-]", dump);
    }
}
