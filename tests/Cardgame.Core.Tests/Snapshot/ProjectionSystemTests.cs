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
            MatchMode = MatchMode.ShuffledMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };
        return MatchSetupSystem.Apply(command, TestCardDesigns.BuildCatalog()).World;
    }

    private static UnitPlayedEvent PlayFirstCard(WorldState world, int slot)
    {
        var player = world.Turn.ActivePlayer;
        return Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, TestCardDesigns.BuildCatalog(), new PlayUnitCommand
        {
            Player = player,
            Card = world.Zones(player).Hand.Cards[0].Id,
            Tier = 1,
            Slot = slot,
        }));
    }

    [Fact]
    public void APlayerSeesTheirOwnHandButOnlyCountsForTheOpponent()
    {
        var world = NewWorld();
        var viewer = world.Turn.StartingPlayer;
        var opponent = PlayerIds.Opponent(viewer);

        var view = ProjectionSystem.Project(world, viewer);

        Assert.True(view.IsOwnTurn);
        Assert.Equal(1, view.Round);
        Assert.Equal(34, view.MaxSeconds);
        Assert.Equal(34, view.OwnSeconds);
        Assert.Equal(34, view.OpponentSeconds);
        Assert.Equal(14, view.OwnLife);
        Assert.Equal(14, view.MaxLife);
        Assert.Null(view.Outcome);
        Assert.Equal(4, view.OwnHandCount);
        Assert.Equal(world.Zones(viewer).Hand.Cards.Select(card => card.Id), view.OwnHandCards);
        Assert.Equal(6, view.OwnDeckCount);
        Assert.Equal(3, view.OpponentHandCount);
        Assert.Equal(7, view.OpponentDeckCount);
        Assert.All(view.OwnHandCards, id => Assert.True(view.Cards.ContainsKey(id)));
        Assert.All(world.Zones(opponent).Hand.Cards, card => Assert.False(view.Cards.ContainsKey(card.Id)));
        Assert.All(world.Zones(viewer).Deck.Cards, card => Assert.False(view.Cards.ContainsKey(card.Id)));
        Assert.Equal(10, view.OwnMana);
        Assert.Equal(10, view.OpponentMaxMana);
        Assert.False(ProjectionSystem.Project(world, opponent).IsOwnTurn);
    }

    [Fact]
    public void TheBoardIsToldFromTheViewersSide()
    {
        var world = NewWorld();
        var player = world.Turn.ActivePlayer;
        var other = PlayerIds.Opponent(player);
        var played = PlayFirstCard(world, 1);

        var viewOfOther = ProjectionSystem.Project(world, other);
        var viewOfPlayer = ProjectionSystem.Project(world, player);

        Assert.Equal(played.Card, viewOfOther.OpponentBoard.UnitSlots[0]);
        Assert.Null(viewOfOther.OwnBoard.UnitSlots[0]);
        Assert.Equal(played.Card, viewOfPlayer.OwnBoard.UnitSlots[0]);
        Assert.Equal(1, viewOfOther.Cards[played.Card].Tier);
        Assert.Equal(world.Board.Side(other).Totems, viewOfOther.OwnBoard.Totems);
        Assert.Equal(world.Board.Side(player).Totems, viewOfOther.OpponentBoard.Totems);
    }

    [Fact]
    public void AViewIsACopyThatLaterChangesDoNotReach()
    {
        var world = NewWorld();
        var player = world.Turn.ActivePlayer;
        var view = ProjectionSystem.Project(world, player);
        var totemsBefore = view.OwnBoard.Totems.ToArray();
        var handBefore = view.OwnHandCards.ToArray();

        PlayFirstCard(world, 3);
        world.Board.Side(player).SetTotemLayout(new[] { TotemType.Mana, TotemType.Time, TotemType.Life });
        TurnSystem.Apply(world, new EndTurnCommand { Player = player });

        Assert.Null(view.OwnBoard.UnitSlots[2]);
        Assert.Equal(totemsBefore, view.OwnBoard.Totems);
        Assert.Equal(handBefore, view.OwnHandCards);
        Assert.Equal(10, view.OwnMana);
        Assert.True(view.IsOwnTurn);
    }
}
