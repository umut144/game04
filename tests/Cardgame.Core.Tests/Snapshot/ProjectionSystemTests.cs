namespace Cardgame.Core.Tests.Snapshot;

using Cardgame.Core.Commands;
using Cardgame.Core.Model;
using Cardgame.Core.Snapshot;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

public sealed class ProjectionSystemTests
{
    [Fact]
    public void APlayerSeesTheirOwnHandButOnlyCountsForTheOpponent()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var deck = TestCardDesigns.BuildDeck(10);
        var command = new SetupMatchCommand
        {
            Seed = 42,
            MirrorMode = MirrorMode.ShuffledMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };
        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        // Moving one card from deck to hand by hand: drawing itself is not
        // G00 scope (docs/TASKS.md). This only exercises the projection's
        // visibility rule.
        var card = world.PlayerA.Deck.Cards[0];
        world.PlayerA.Hand.Add(card);

        var view = ProjectionSystem.Project(world, PlayerId.PlayerA);

        Assert.Equal(1, view.OwnHandCount);
        Assert.Contains(card.Id, view.OwnHandCards);
        Assert.Equal(10, view.OwnDeckCount);
        Assert.Equal(0, view.OpponentHandCount);
        Assert.Equal(10, view.OpponentDeckCount);
    }
}
