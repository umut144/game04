namespace Cardgame.Core.Tests.Snapshot;

using Cardgame.Core.Commands;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Snapshot;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

public sealed class ProjectionSystemTests
{
    private static WorldState NewWorld(int deckSize)
    {
        var deck = TestCardDesigns.BuildDeck(deckSize);
        var command = new SetupMatchCommand
        {
            Seed = 42,
            MirrorMode = MirrorMode.ShuffledMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };
        return MatchSetupSystem.Apply(command, TestCardDesigns.BuildCatalog()).World;
    }

    [Fact]
    public void APlayerSeesTheirOwnHandButOnlyCountsForTheOpponent()
    {
        var world = NewWorld(10);

        // Moving one card from deck to hand by hand: drawing is not built yet
        // (CORE-10). This only exercises the projection's visibility rule.
        var card = world.PlayerA.Deck.Cards[0];
        world.PlayerA.Hand.Add(card);

        var view = ProjectionSystem.Project(world, PlayerId.PlayerA);

        Assert.Equal(1, view.OwnHandCount);
        Assert.Contains(card.Id, view.OwnHandCards);
        Assert.Equal(10, view.OwnDeckCount);
        Assert.Equal(0, view.OpponentHandCount);
        Assert.Equal(10, view.OpponentDeckCount);
    }

    [Fact]
    public void TheBoardIsToldFromTheViewersSide()
    {
        var world = NewWorld(0);
        var placed = (BlankCardPlacedEvent)BoardPlacementSystem.Apply(
            world, new PlaceBlankCardCommand { Player = PlayerId.PlayerB, Slot = 1 });

        var viewOfA = ProjectionSystem.Project(world, PlayerId.PlayerA);
        var viewOfB = ProjectionSystem.Project(world, PlayerId.PlayerB);

        Assert.Equal(placed.Card, viewOfA.OpponentBoard.UnitSlots[0]);
        Assert.Null(viewOfA.OwnBoard.UnitSlots[0]);
        Assert.Equal(placed.Card, viewOfB.OwnBoard.UnitSlots[0]);
        Assert.Equal(world.Board.PlayerA.Totems, viewOfA.OwnBoard.Totems);
        Assert.Equal(world.Board.PlayerB.Totems, viewOfA.OpponentBoard.Totems);
    }

    [Fact]
    public void AViewIsACopyThatLaterChangesDoNotReach()
    {
        var world = NewWorld(0);
        var view = ProjectionSystem.Project(world, PlayerId.PlayerA);
        var totemsBefore = view.OwnBoard.Totems.ToArray();

        BoardPlacementSystem.Apply(world, new PlaceBlankCardCommand { Player = PlayerId.PlayerA, Slot = 3 });
        world.Board.PlayerA.SetTotemLayout(new[] { TotemType.Mana, TotemType.Time, TotemType.Life });

        Assert.Null(view.OwnBoard.UnitSlots[2]);
        Assert.Equal(totemsBefore, view.OwnBoard.Totems);
    }
}
