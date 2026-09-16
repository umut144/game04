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
    private static WorldState NewWorld()
    {
        var deck = TestCardDesigns.BuildDeck(10);
        var command = new SetupMatchCommand
        {
            Seed = 42,
            MirrorMode = MirrorMode.ShuffledMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };
        return MatchSetupSystem.Apply(command, TestCardDesigns.BuildCatalog()).World;
    }

    private static UnitPlayedEvent PlayFirstCard(WorldState world, PlayerId player, int slot) =>
        Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, TestCardDesigns.BuildCatalog(), new PlayUnitCommand
        {
            Player = player,
            Card = world.Zones(player).Hand.Cards[0].Id,
            Tier = 1,
            Slot = slot,
        }));

    [Fact]
    public void APlayerSeesTheirOwnHandButOnlyCountsForTheOpponent()
    {
        var world = NewWorld();

        var view = ProjectionSystem.Project(world, PlayerId.PlayerA);

        Assert.Equal(4, view.OwnHandCount);
        Assert.Equal(world.PlayerA.Hand.Cards.Select(card => card.Id), view.OwnHandCards);
        Assert.Equal(6, view.OwnDeckCount);
        Assert.Equal(4, view.OpponentHandCount);
        Assert.Equal(6, view.OpponentDeckCount);
        Assert.All(view.OwnHandCards, id => Assert.True(view.Cards.ContainsKey(id)));
        Assert.All(world.PlayerB.Hand.Cards, card => Assert.False(view.Cards.ContainsKey(card.Id)));
        Assert.All(world.PlayerA.Deck.Cards, card => Assert.False(view.Cards.ContainsKey(card.Id)));
        Assert.Equal(7, view.OwnMana);
        Assert.Equal(7, view.OpponentMaxMana);
    }

    [Fact]
    public void TheBoardIsToldFromTheViewersSide()
    {
        var world = NewWorld();
        var played = PlayFirstCard(world, PlayerId.PlayerB, 1);

        var viewOfA = ProjectionSystem.Project(world, PlayerId.PlayerA);
        var viewOfB = ProjectionSystem.Project(world, PlayerId.PlayerB);

        Assert.Equal(played.Card, viewOfA.OpponentBoard.UnitSlots[0]);
        Assert.Null(viewOfA.OwnBoard.UnitSlots[0]);
        Assert.Equal(played.Card, viewOfB.OwnBoard.UnitSlots[0]);
        Assert.Equal(1, viewOfA.Cards[played.Card].Tier);
        Assert.Equal(world.Board.PlayerA.Totems, viewOfA.OwnBoard.Totems);
        Assert.Equal(world.Board.PlayerB.Totems, viewOfA.OpponentBoard.Totems);
    }

    [Fact]
    public void AViewIsACopyThatLaterChangesDoNotReach()
    {
        var world = NewWorld();
        var view = ProjectionSystem.Project(world, PlayerId.PlayerA);
        var totemsBefore = view.OwnBoard.Totems.ToArray();
        var handBefore = view.OwnHandCards.ToArray();

        PlayFirstCard(world, PlayerId.PlayerA, 3);
        world.Board.PlayerA.SetTotemLayout(new[] { TotemType.Mana, TotemType.Time, TotemType.Life });

        Assert.Null(view.OwnBoard.UnitSlots[2]);
        Assert.Equal(totemsBefore, view.OwnBoard.Totems);
        Assert.Equal(handBefore, view.OwnHandCards);
        Assert.Equal(7, view.OwnMana);
    }
}
