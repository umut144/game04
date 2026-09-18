namespace Cardgame.Core.Tests.Systems;

using Cardgame.Core.Commands;
using Cardgame.Core.Design;
using Cardgame.Core.Diagnostics;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

public sealed class TurnSystemTests
{
    private static readonly CardCatalog Catalog = TestCardDesigns.BuildCatalog();

    private static WorldState NewWorld(MatchMode mode, int deckSize = 20, ulong seed = 7) =>
        MatchSetupSystem.Apply(
            new SetupMatchCommand
            {
                Seed = seed,
                MatchMode = mode,
                PlayerADeckDefinitionIds = Enumerable.Repeat(TestCardDesigns.CardAId, deckSize).ToArray(),
                PlayerBDeckDefinitionIds = Enumerable.Repeat(TestCardDesigns.CardAId, deckSize).ToArray(),
            },
            Catalog).World;

    private static TurnStartedEvent EndTurn(WorldState world, bool timedOut = false) =>
        Assert.IsType<TurnStartedEvent>(TurnSystem.Apply(world, new EndTurnCommand { Player = world.Turn.ActivePlayer, TimedOut = timedOut }));

    [Fact]
    public void TheStartingPlayerComesFromTheSeedAndBothSidesGetToStart()
    {
        var starters = Enumerable.Range(0, 32).Select(seed => NewWorld(MatchMode.ShuffledMirror, seed: (ulong)seed).Turn.StartingPlayer).ToArray();

        Assert.Contains(PlayerId.PlayerA, starters);
        Assert.Contains(PlayerId.PlayerB, starters);
        Assert.Equal(starters[5], NewWorld(MatchMode.ShuffledMirror, seed: 5).Turn.StartingPlayer);
        Assert.Equal(starters[5], NewWorld(MatchMode.PerfectMirror, seed: 5).Turn.StartingPlayer);
    }

    [Fact]
    public void TurnsAlternateAndEachStartsWithADraw()
    {
        var world = NewWorld(MatchMode.ShuffledMirror);
        var starter = world.Turn.StartingPlayer;
        var second = PlayerIds.Opponent(starter);

        var secondTurn = EndTurn(world);

        Assert.Equal(second, secondTurn.Player);
        Assert.Equal(starter, secondTurn.Previous);
        Assert.Equal(1, secondTurn.Round);
        Assert.NotNull(secondTurn.Drawn);
        Assert.Equal(4, world.Zones(second).Hand.Cards.Count);
        Assert.Equal(4, world.Zones(starter).Hand.Cards.Count);

        var thirdTurn = EndTurn(world, timedOut: true);

        Assert.Equal(starter, thirdTurn.Player);
        Assert.True(thirdTurn.PreviousTimedOut);
        Assert.Equal(2, thirdTurn.Round);
        Assert.Equal(5, world.Zones(starter).Hand.Cards.Count);
    }

    [Fact]
    public void OnlyTheActivePlayerMayEndTheTurnOrPlay()
    {
        var world = NewWorld(MatchMode.ShuffledMirror);
        var waiting = PlayerIds.Opponent(world.Turn.ActivePlayer);
        string before = WorldStateDumper.Dump(world);

        Assert.IsType<CommandRejectedEvent>(TurnSystem.Apply(world, new EndTurnCommand { Player = waiting }));
        Assert.IsType<CommandRejectedEvent>(CardPlaySystem.Apply(world, Catalog, new PlayUnitCommand
        {
            Player = waiting,
            Card = world.Zones(waiting).Hand.Cards[0].Id,
            Tier = 1,
            Slot = 1,
        }));

        Assert.Equal(before, WorldStateDumper.Dump(world));
    }

    [Theory]
    [InlineData(MatchMode.ShuffledMirror)]
    [InlineData(MatchMode.PerfectMirror)]
    public void MirrorModesStartEveryTurnWithFullMana(MatchMode mode)
    {
        var world = NewWorld(mode);
        var starter = world.Turn.StartingPlayer;
        CardPlaySystem.Apply(world, Catalog, new PlayUnitCommand
        {
            Player = starter,
            Card = world.Zones(starter).Hand.Cards[0].Id,
            Tier = 3,
            Slot = 1,
        });
        Assert.Equal(7, world.Zones(starter).Mana.Current);

        EndTurn(world);
        Assert.Equal(10, world.Zones(PlayerIds.Opponent(starter)).Mana.Current);
        EndTurn(world);
        Assert.Equal(10, world.Zones(starter).Mana.Current);
    }

    [Fact]
    public void ConstructedGrowsManaByOnePerRoundUpToTheMaximumAndDropsWhatIsLeft()
    {
        var world = NewWorld(MatchMode.Constructed);
        var starter = world.Turn.StartingPlayer;
        var second = PlayerIds.Opponent(starter);
        // Both are paid for their first turn at setup (G06-02).
        Assert.Equal(1, world.Zones(starter).Mana.Current);
        Assert.Equal(1, world.Zones(second).Mana.Current);

        var seen = new List<int>();
        for (int round = 1; round <= 12; round++)
        {
            // Keep the quiet-round counter fresh: the draw after eight quiet
            // rounds is G06-05's rule and has its own test.
            world.Turn.LastLifeDamageRound = world.Turn.Round;
            Assert.Equal(round, world.Turn.Round);
            seen.Add(world.Zones(starter).Mana.Current);
            EndTurn(world);
            Assert.Equal(Math.Min(round, 10), world.Zones(second).Mana.Current);
            EndTurn(world);
        }

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 10 }, seen);
    }

    [Fact]
    public void AFullHandOrAnEmptyDeckDrawsNothing()
    {
        var world = NewWorld(MatchMode.ShuffledMirror, deckSize: 9);
        var starter = world.Turn.StartingPlayer;
        for (int i = 0; i < 12; i++)
        {
            EndTurn(world);
        }

        // 9 cards: 3 dealt, then one per own turn until the hand is at 8;
        // the ninth stays in the deck because the hand is full.
        Assert.Equal(8, world.Zones(starter).Hand.Cards.Count);
        Assert.Single(world.Zones(starter).Deck.Cards);

        var tiny = NewWorld(MatchMode.ShuffledMirror, deckSize: 3);
        var turn = EndTurn(tiny);
        Assert.Null(turn.Drawn);
        Assert.Equal(3, tiny.Zones(turn.Player).Hand.Cards.Count);
    }
}
