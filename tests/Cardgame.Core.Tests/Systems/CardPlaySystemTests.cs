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
                MatchMode = MatchMode.ShuffledMirror,
                PlayerADeckDefinitionIds = deck,
                PlayerBDeckDefinitionIds = deck,
            },
            Catalog).World;
    }

    private static PlayUnitCommand Play(WorldState world, int handIndex, int tier, int slot) => new()
    {
        Player = world.Turn.ActivePlayer,
        Card = Active(world).Hand.Cards[handIndex].Id,
        Tier = tier,
        Slot = slot,
    };

    private static Cardgame.Core.Zones.PlayerZones Active(WorldState world) => world.Zones(world.Turn.ActivePlayer);

    private static Cardgame.Core.Board.BoardSide ActiveSide(WorldState world) => world.Board.Side(world.Turn.ActivePlayer);

    [Fact]
    public void PlayingAUnitMovesItFromHandToTheSlotAtItsTierAndPaysTheCost()
    {
        var world = NewWorld();
        var command = Play(world, 0, 3, 5);

        var played = Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog, command));

        Assert.Equal(4, played.ManaPaid);
        Assert.Equal(6, Active(world).Mana.Current);
        Assert.Equal(3, Active(world).Hand.Cards.Count);
        Assert.Equal(command.Card, ActiveSide(world).OccupantOf(5));
        Assert.Equal(3, world.Board.Units[command.Card].Tier);
    }

    [Fact]
    public void ATierThatCostsMoreThanTheManaLeftIsRefused()
    {
        var world = NewWorld();
        Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog, Play(world, 0, 3, 1)));
        Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog, Play(world, 0, 3, 2)));
        Assert.Equal(2, Active(world).Mana.Current);
        string before = WorldStateDumper.Dump(world);

        var result = CardPlaySystem.Apply(world, Catalog, Play(world, 0, 3, 3));

        Assert.Contains("costs 4 mana, 2 available", Assert.IsType<CommandRejectedEvent>(result).Reason);
        Assert.Equal(before, WorldStateDumper.Dump(world));
        Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog, Play(world, 0, 2, 3)));
        Assert.Equal(0, Active(world).Mana.Current);
    }

    [Fact]
    public void BadCommandsAreRejectedAndChangeNothing()
    {
        var world = NewWorld();
        CardPlaySystem.Apply(world, Catalog, Play(world, 0, 1, 2));
        string before = WorldStateDumper.Dump(world);
        var player = world.Turn.ActivePlayer;
        var opponentCard = world.Zones(PlayerIds.Opponent(player)).Hand.Cards[0].Id;

        var commands = new[]
        {
            Play(world, 0, 1, 2),
            Play(world, 0, 0, 1),
            Play(world, 0, 4, 1),
            Play(world, 0, 1, 0),
            Play(world, 0, 1, 7),
            new PlayUnitCommand { Player = player, Card = opponentCard, Tier = 1, Slot = 1 },
            new PlayUnitCommand { Player = player, Card = new CardInstanceId(999), Tier = 1, Slot = 1 },
            new PlayUnitCommand { Player = PlayerIds.Opponent(player), Card = opponentCard, Tier = 1, Slot = 4 },
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
            CardPlaySystem.Apply(world, new RefillManaCommand { Player = world.Turn.ActivePlayer }));

        Assert.Equal(10, refilled.Mana);
        Assert.Equal(10, Active(world).Mana.Current);
    }
}
