namespace Cardgame.Core.Tests.Systems;

using Cardgame.Core.Commands;
using Cardgame.Core.Diagnostics;
using Cardgame.Core.Model;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

public sealed class MatchSetupSystemTests
{
    [Fact]
    public void PerfectMirrorGivesBothSidesTheIdenticalDeckOrder()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var deck = TestCardDesigns.BuildDeck(10);
        var command = new SetupMatchCommand
        {
            Seed = 12345,
            MatchMode = MatchMode.PerfectMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };

        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        // Hand and deck together are the shuffled deck; the starting player
        // simply drew one card more.
        var playerAOrder = world.PlayerA.Hand.Cards.Concat(world.PlayerA.Deck.Cards).Select(card => card.DefinitionId).ToArray();
        var playerBOrder = world.PlayerB.Hand.Cards.Concat(world.PlayerB.Deck.Cards).Select(card => card.DefinitionId).ToArray();

        Assert.Equal(playerAOrder, playerBOrder);
    }

    [Fact]
    public void ShuffledMirrorGivesTheTwoSidesIndependentDeckOrders()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var deck = TestCardDesigns.BuildDeck(20);
        var command = new SetupMatchCommand
        {
            Seed = 999,
            MatchMode = MatchMode.ShuffledMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };

        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        // Hand and deck together are the shuffled deck; the starting player
        // simply drew one card more.
        var playerAOrder = world.PlayerA.Hand.Cards.Concat(world.PlayerA.Deck.Cards).Select(card => card.DefinitionId).ToArray();
        var playerBOrder = world.PlayerB.Hand.Cards.Concat(world.PlayerB.Deck.Cards).Select(card => card.DefinitionId).ToArray();

        Assert.NotEqual(playerAOrder, playerBOrder);
    }

    [Fact]
    public void TheSameSeedReproducesTheExactSameWorldEveryTime()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var deck = TestCardDesigns.BuildDeck(15);
        var command = new SetupMatchCommand
        {
            Seed = 555,
            MatchMode = MatchMode.ShuffledMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };

        var (firstWorld, _) = MatchSetupSystem.Apply(command, catalog);
        var (secondWorld, _) = MatchSetupSystem.Apply(command, catalog);

        Assert.Equal(WorldStateDumper.Dump(firstWorld), WorldStateDumper.Dump(secondWorld));

        var firstOrder = firstWorld.PlayerA.Deck.Cards.Select(card => card.DefinitionId).ToArray();
        var secondOrder = secondWorld.PlayerA.Deck.Cards.Select(card => card.DefinitionId).ToArray();
        Assert.Equal(firstOrder, secondOrder);
    }

    [Fact]
    public void BothSidesGetThreeCardsAndTheStartingPlayerDrawsAFourth()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var deck = TestCardDesigns.BuildDeck(10);
        var command = new SetupMatchCommand
        {
            Seed = 1,
            MatchMode = MatchMode.PerfectMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };

        var (world, setUp) = MatchSetupSystem.Apply(command, catalog);

        var starter = world.Zones(world.Turn.StartingPlayer);
        var other = world.Zones(PlayerIds.Opponent(world.Turn.StartingPlayer));
        Assert.Equal(world.Turn.StartingPlayer, world.Turn.ActivePlayer);
        Assert.Equal(1, world.Turn.Round);
        Assert.Equal(4, starter.Hand.Cards.Count);
        Assert.Equal(6, starter.Deck.Cards.Count);
        Assert.Equal(3, other.Hand.Cards.Count);
        Assert.Equal(7, other.Deck.Cards.Count);
        Assert.Equal(starter.Hand.Cards[3].Id, setUp.FirstTurn.Drawn);
        Assert.All(starter.Hand.Cards, card => Assert.Null(card.Tier));
        Assert.Equal(10, world.PlayerA.Mana.Current);
        Assert.Equal(10, world.PlayerB.Mana.Current);
    }

    [Fact]
    public void AShortDeckDealsWhatItHas()
    {
        var command = new SetupMatchCommand
        {
            Seed = 3,
            MatchMode = MatchMode.ShuffledMirror,
            PlayerADeckDefinitionIds = TestCardDesigns.BuildDeck(2),
            PlayerBDeckDefinitionIds = Array.Empty<string>(),
        };

        var (world, _) = MatchSetupSystem.Apply(command, TestCardDesigns.BuildCatalog());

        Assert.Equal(2, world.PlayerA.Hand.Cards.Count);
        Assert.Empty(world.PlayerA.Deck.Cards);
        Assert.Empty(world.PlayerB.Hand.Cards);
    }

    [Fact]
    public void EachSideStartsWithSixEmptySlotsAndThreeTotems()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var command = new SetupMatchCommand
        {
            Seed = 1,
            MatchMode = MatchMode.PerfectMirror,
            PlayerADeckDefinitionIds = Array.Empty<string>(),
            PlayerBDeckDefinitionIds = Array.Empty<string>(),
        };

        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        Assert.Equal(6, world.Board.PlayerA.UnitSlots.Count);
        Assert.Equal(3, world.Board.PlayerA.TotemColumnPairs.Count);
        Assert.Equal(6, world.Board.PlayerB.UnitSlots.Count);
        Assert.Equal(3, world.Board.PlayerB.TotemColumnPairs.Count);
        Assert.Equal(3, world.Board.PlayerA.Totems.Count);
        Assert.Equal(3, world.Board.PlayerB.Totems.Count);
        Assert.All(world.Board.PlayerA.UnitSlots, slot => Assert.Null(slot));
    }
}
