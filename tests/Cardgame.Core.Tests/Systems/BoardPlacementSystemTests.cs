namespace Cardgame.Core.Tests.Systems;

using Cardgame.Core.Commands;
using Cardgame.Core.Diagnostics;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

public sealed class BoardPlacementSystemTests
{
    private static WorldState NewWorld(int deckSize = 0)
    {
        var deck = TestCardDesigns.BuildDeck(deckSize);
        return MatchSetupSystem.Apply(
            new SetupMatchCommand
            {
                Seed = 1,
                MirrorMode = MirrorMode.ShuffledMirror,
                PlayerADeckDefinitionIds = deck,
                PlayerBDeckDefinitionIds = deck,
            },
            TestCardDesigns.BuildCatalog()).World;
    }

    [Fact]
    public void ABlankCardTakesAFreeSlotWithAFreshId()
    {
        var world = NewWorld(deckSize: 4);
        var deckIds = world.PlayerA.Deck.Cards.Concat(world.PlayerB.Deck.Cards).Select(c => c.Id).ToHashSet();

        var result = BoardPlacementSystem.Apply(world, new PlaceBlankCardCommand { Player = PlayerId.PlayerB, Slot = 4 });

        var placed = Assert.IsType<BlankCardPlacedEvent>(result);
        Assert.Equal(PlayerId.PlayerB, placed.Player);
        Assert.Equal(4, placed.Slot);
        Assert.Equal(placed.Card, world.Board.PlayerB.OccupantOf(4));
        Assert.DoesNotContain(placed.Card, deckIds);
        Assert.False(world.Board.PlayerA.IsOccupied(4));
    }

    [Fact]
    public void AnOccupiedSlotRejectsASecondCardAndStaysAsItWas()
    {
        var world = NewWorld();
        var first = BoardPlacementSystem.Apply(world, new PlaceBlankCardCommand { Player = PlayerId.PlayerA, Slot = 2 });
        string before = WorldStateDumper.Dump(world);

        var result = BoardPlacementSystem.Apply(world, new PlaceBlankCardCommand { Player = PlayerId.PlayerA, Slot = 2 });

        Assert.IsType<CommandRejectedEvent>(result);
        Assert.Equal(before, WorldStateDumper.Dump(world));
        Assert.Equal(((BlankCardPlacedEvent)first).Card, world.Board.PlayerA.OccupantOf(2));
    }

    [Fact]
    public void ClearingASlotEmptiesItAndNamesTheCard()
    {
        var world = NewWorld();
        var placed = (BlankCardPlacedEvent)BoardPlacementSystem.Apply(
            world, new PlaceBlankCardCommand { Player = PlayerId.PlayerA, Slot = 5 });

        var result = BoardPlacementSystem.Apply(world, new ClearSlotCommand { Player = PlayerId.PlayerA, Slot = 5 });

        var cleared = Assert.IsType<SlotClearedEvent>(result);
        Assert.Equal(placed.Card, cleared.Card);
        Assert.False(world.Board.PlayerA.IsOccupied(5));
    }

    [Fact]
    public void BadSlotsAndEmptySlotsAreRejectedNotThrown()
    {
        var world = NewWorld();

        Assert.IsType<CommandRejectedEvent>(
            BoardPlacementSystem.Apply(world, new PlaceBlankCardCommand { Player = PlayerId.PlayerA, Slot = 0 }));
        Assert.IsType<CommandRejectedEvent>(
            BoardPlacementSystem.Apply(world, new PlaceBlankCardCommand { Player = PlayerId.PlayerA, Slot = 7 }));
        Assert.IsType<CommandRejectedEvent>(
            BoardPlacementSystem.Apply(world, new ClearSlotCommand { Player = PlayerId.PlayerA, Slot = 3 }));
        Assert.IsType<CommandRejectedEvent>(
            BoardPlacementSystem.Apply(world, new ClearSlotCommand { Player = PlayerId.PlayerA, Slot = 9 }));
    }
}
