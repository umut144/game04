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
            MirrorMode = MirrorMode.PerfectMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };

        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        var playerAOrder = world.PlayerA.Deck.Cards.Select(card => card.DefinitionId).ToArray();
        var playerBOrder = world.PlayerB.Deck.Cards.Select(card => card.DefinitionId).ToArray();

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
            MirrorMode = MirrorMode.ShuffledMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };

        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        var playerAOrder = world.PlayerA.Deck.Cards.Select(card => card.DefinitionId).ToArray();
        var playerBOrder = world.PlayerB.Deck.Cards.Select(card => card.DefinitionId).ToArray();

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
            MirrorMode = MirrorMode.ShuffledMirror,
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
    public void MatchSetupDoesNotDealAStartingHand()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var deck = TestCardDesigns.BuildDeck(10);
        var command = new SetupMatchCommand
        {
            Seed = 1,
            MirrorMode = MirrorMode.PerfectMirror,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };

        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        Assert.Empty(world.PlayerA.Hand.Cards);
        Assert.Empty(world.PlayerB.Hand.Cards);
        Assert.Equal(10, world.PlayerA.Deck.Cards.Count);
    }

    [Fact]
    public void BoardGeometryHasSixSlotsAndThreeAnonymousTotemColumnPairsPerSide()
    {
        var catalog = TestCardDesigns.BuildCatalog();
        var command = new SetupMatchCommand
        {
            Seed = 1,
            MirrorMode = MirrorMode.PerfectMirror,
            PlayerADeckDefinitionIds = Array.Empty<string>(),
            PlayerBDeckDefinitionIds = Array.Empty<string>(),
        };

        var (world, _) = MatchSetupSystem.Apply(command, catalog);

        Assert.Equal(6, world.Board.PlayerA.UnitSlots.Count);
        Assert.Equal(3, world.Board.PlayerA.TotemColumnPairs.Count);
        Assert.Equal(6, world.Board.PlayerB.UnitSlots.Count);
        Assert.Equal(3, world.Board.PlayerB.TotemColumnPairs.Count);
    }
}
