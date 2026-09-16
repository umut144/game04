namespace Cardgame.Core.Tests.Systems;

using Cardgame.Core.Board;
using Cardgame.Core.Commands;
using Cardgame.Core.Model;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

public sealed class TotemLayoutTests
{
    private static WorldState SetUp(ulong seed, MatchMode mode, int deckSize = 0)
    {
        var deck = TestCardDesigns.BuildDeck(deckSize);
        var command = new SetupMatchCommand
        {
            Seed = seed,
            MatchMode = mode,
            PlayerADeckDefinitionIds = deck,
            PlayerBDeckDefinitionIds = deck,
        };
        return MatchSetupSystem.Apply(command, TestCardDesigns.BuildCatalog()).World;
    }

    private static string Layout(BoardSide side) =>
        string.Join(",", side.Totems.Select(t => $"{t.Position}:{t.Type}"));

    [Fact]
    public void EverySideCarriesEachTotemOnceOnPlacesAToC()
    {
        foreach (var mode in Enum.GetValues<MatchMode>())
        {
            var world = SetUp(7, mode);
            foreach (var side in new[] { world.Board.PlayerA, world.Board.PlayerB })
            {
                Assert.Equal(new[] { TotemPosition.A, TotemPosition.B, TotemPosition.C }, side.Totems.Select(t => t.Position));
                Assert.Equal(
                    Enum.GetValues<TotemType>().OrderBy(t => t),
                    side.Totems.Select(t => t.Type).OrderBy(t => t));
            }
        }
    }

    [Fact]
    public void PerfectMirrorGivesBothSidesTheSameLayout()
    {
        for (ulong seed = 0; seed < 32; seed++)
        {
            var world = SetUp(seed, MatchMode.PerfectMirror);
            Assert.Equal(Layout(world.Board.PlayerA), Layout(world.Board.PlayerB));
        }
    }

    [Fact]
    public void ShuffledMirrorRollsEachSideOnItsOwn()
    {
        // Independent rolls differ most of the time and coincide now and
        // then (1 in 6); over 64 fixed seeds both must happen.
        int same = 0;
        int different = 0;
        for (ulong seed = 0; seed < 64; seed++)
        {
            var world = SetUp(seed, MatchMode.ShuffledMirror);
            if (Layout(world.Board.PlayerA) == Layout(world.Board.PlayerB))
            {
                same++;
            }
            else
            {
                different++;
            }
        }

        Assert.True(same > 0, "independent layouts never coincided");
        Assert.True(different > same, "independent layouts coincided too often");
    }

    [Fact]
    public void TheLayoutIsRandomisedAcrossSeeds()
    {
        var layouts = new HashSet<string>();
        for (ulong seed = 0; seed < 64; seed++)
        {
            layouts.Add(Layout(SetUp(seed, MatchMode.PerfectMirror).Board.PlayerA));
        }

        Assert.Equal(6, layouts.Count);
    }

    [Fact]
    public void TheSameSeedGivesTheSameLayout()
    {
        var first = SetUp(4242, MatchMode.ShuffledMirror);
        var second = SetUp(4242, MatchMode.ShuffledMirror);

        Assert.Equal(Layout(first.Board.PlayerA), Layout(second.Board.PlayerA));
        Assert.Equal(Layout(first.Board.PlayerB), Layout(second.Board.PlayerB));
    }

    [Fact]
    public void TheDecksDoNotMoveTheTotems()
    {
        var withoutCards = SetUp(99, MatchMode.ShuffledMirror, deckSize: 0);
        var withCards = SetUp(99, MatchMode.ShuffledMirror, deckSize: 20);

        Assert.Equal(Layout(withoutCards.Board.PlayerA), Layout(withCards.Board.PlayerA));
        Assert.Equal(Layout(withoutCards.Board.PlayerB), Layout(withCards.Board.PlayerB));
    }

    [Fact]
    public void OneSideCanDeviateWithoutTouchingTheOther()
    {
        var world = SetUp(5, MatchMode.PerfectMirror);
        string opponentBefore = Layout(world.Board.PlayerB);

        world.Board.PlayerA.SetTotemLayout(new[] { TotemType.Time, TotemType.Life, TotemType.Mana });

        Assert.Equal(TotemType.Time, world.Board.PlayerA.TotemAt(TotemPosition.A));
        Assert.Equal(TotemPosition.B, world.Board.PlayerA.PositionOf(TotemType.Life));
        Assert.Equal(opponentBefore, Layout(world.Board.PlayerB));
    }

    [Fact]
    public void ALayoutMustNameEachTotemOnce()
    {
        var side = new BoardSide();

        Assert.Throws<ArgumentException>(() => side.SetTotemLayout(new[] { TotemType.Life, TotemType.Life, TotemType.Mana }));
        Assert.Throws<ArgumentException>(() => side.SetTotemLayout(new[] { TotemType.Life, TotemType.Mana }));
        Assert.Throws<InvalidOperationException>(() => side.TotemAt(TotemPosition.A));
    }
}
