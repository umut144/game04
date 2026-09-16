namespace Cardgame.Core.Tests.Systems;

using Cardgame.Core.Commands;
using Cardgame.Core.Diagnostics;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

public sealed class CardPlaySystemTests
{
    private static readonly Cardgame.Core.Design.CardCatalog Catalog = TestCardDesigns.BuildCatalog();

    private static WorldState NewWorld()
    {
        // Test card B only: its tiers cost 1, 2 and 4.
        var deck = Enumerable.Repeat(TestCardDesigns.CardBId, 10).ToArray();
        return MatchSetupSystem.Apply(
            new SetupMatchCommand
            {
                Seed = 1,
                MirrorMode = MirrorMode.ShuffledMirror,
                PlayerADeckDefinitionIds = deck,
                PlayerBDeckDefinitionIds = deck,
            },
            Catalog).World;
    }

    private static PlayUnitCommand Play(WorldState world, int handIndex, int tier, int slot) => new()
    {
        Player = PlayerId.PlayerA,
        Card = world.PlayerA.Hand.Cards[handIndex].Id,
        Tier = tier,
        Slot = slot,
    };

    [Fact]
    public void PlayingAUnitMovesItFromHandToTheSlotAtItsTierAndPaysTheCost()
    {
        var world = NewWorld();
        var command = Play(world, 0, 3, 5);

        var played = Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog, command));

        Assert.Equal(4, played.ManaPaid);
        Assert.Equal(3, world.PlayerA.Mana.Current);
        Assert.Equal(3, world.PlayerA.Hand.Cards.Count);
        Assert.Equal(command.Card, world.Board.PlayerA.OccupantOf(5));
        Assert.Equal(3, world.Board.Units[command.Card].Tier);
    }

    [Fact]
    public void ATierThatCostsMoreThanTheManaLeftIsRefused()
    {
        var world = NewWorld();
        Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog, Play(world, 0, 3, 1)));
        Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog, Play(world, 0, 2, 2)));
        string before = WorldStateDumper.Dump(world);

        var result = CardPlaySystem.Apply(world, Catalog, Play(world, 0, 2, 3));

        Assert.Contains("costs 2 mana, 1 available", Assert.IsType<CommandRejectedEvent>(result).Reason);
        Assert.Equal(before, WorldStateDumper.Dump(world));
        Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog, Play(world, 0, 1, 3)));
        Assert.Equal(0, world.PlayerA.Mana.Current);
    }

    [Fact]
    public void BadCommandsAreRejectedAndChangeNothing()
    {
        var world = NewWorld();
        CardPlaySystem.Apply(world, Catalog, Play(world, 0, 1, 2));
        string before = WorldStateDumper.Dump(world);
        var opponentCard = world.PlayerB.Hand.Cards[0].Id;

        var commands = new[]
        {
            Play(world, 0, 1, 2),
            Play(world, 0, 0, 1),
            Play(world, 0, 4, 1),
            Play(world, 0, 1, 0),
            Play(world, 0, 1, 7),
            new PlayUnitCommand { Player = PlayerId.PlayerA, Card = opponentCard, Tier = 1, Slot = 1 },
            new PlayUnitCommand { Player = PlayerId.PlayerA, Card = new CardInstanceId(999), Tier = 1, Slot = 1 },
        };

        foreach (var command in commands)
        {
            Assert.IsType<CommandRejectedEvent>(CardPlaySystem.Apply(world, Catalog, command));
        }

        Assert.Equal(before, WorldStateDumper.Dump(world));
    }

    [Fact]
    public void RefillingManaRestoresTheMaximum()
    {
        var world = NewWorld();
        CardPlaySystem.Apply(world, Catalog, Play(world, 0, 3, 1));

        var refilled = Assert.IsType<ManaRefilledEvent>(
            CardPlaySystem.Apply(world, new RefillManaCommand { Player = PlayerId.PlayerA }));

        Assert.Equal(7, refilled.Mana);
        Assert.Equal(7, world.PlayerA.Mana.Current);
    }
}
