namespace Cardgame.Core.Tests.Systems;

using Cardgame.Core.Board;
using Cardgame.Core.Combat;
using Cardgame.Core.Commands;
using Cardgame.Core.Design;
using Cardgame.Core.Diagnostics;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Systems;
using Cardgame.TestSupport;
using Xunit;

/// <summary>
/// Totems under attack (G06): protection by column, the Rogue's bypass, the
/// diagonal surcharge, what each totem loses, and how the match ends.
/// </summary>
public sealed class TotemCombatTests
{
    private static readonly CardCatalog Catalog = CombatCards.BuildCatalog();
    private const PlayerId A = PlayerId.PlayerA;
    private const PlayerId B = PlayerId.PlayerB;

    // An empty board, A to move, both sides' totems in a known order:
    // place A is Life, B is Mana, C is Time.
    private static WorldState NewWorld(MatchMode mode = MatchMode.ShuffledMirror)
    {
        WorldState world;
        ulong seed = 0;
        do
        {
            world = MatchSetupSystem.Apply(
                new SetupMatchCommand
                {
                    Seed = seed++,
                    MatchMode = mode,
                    PlayerADeckDefinitionIds = Array.Empty<string>(),
                    PlayerBDeckDefinitionIds = Array.Empty<string>(),
                },
                Catalog).World;
        }
        while (world.Turn.ActivePlayer != A);

        var layout = new[] { TotemType.Life, TotemType.Mana, TotemType.Time };
        world.Board.PlayerA.SetTotemLayout(layout);
        world.Board.PlayerB.SetTotemLayout(layout);
        return world;
    }

    private static CardInstance Put(WorldState world, PlayerId side, int slot, string id)
    {
        var card = new CardInstance(world.CardIds.Next(), id);
        CardPlaySystem.PlaceOnBoard(world, side, slot, card, 1, ready: true);
        return card;
    }

    private static IEvent Attack(WorldState world, int from, TotemPosition position, PlayerId side = B) =>
        CombatSystem.Apply(world, Catalog, new AttackCommand
        {
            Player = A,
            AttackerSlot = from,
            TotemTarget = new TotemRef(side, position),
        });

    [Fact]
    public void AUnitInEitherColumnProtectsTheTotemBehindThem()
    {
        var world = NewWorld();
        Put(world, A, 1, "ranger");

        Assert.Equal(
            new[] { TotemPosition.A, TotemPosition.B },
            CombatRules.TotemTargets(world, Catalog, A, 1).Select(totem => totem.Position));

        Put(world, B, 2, "wall");

        // Slot 2 belongs to place A's columns (1-2), so only that totem closes.
        Assert.Equal(
            new[] { TotemPosition.B },
            CombatRules.TotemTargets(world, Catalog, A, 1).Select(totem => totem.Position));
        Assert.IsType<CommandRejectedEvent>(Attack(world, 1, TotemPosition.A));
    }

    [Fact]
    public void BypassNeedsOnlyTheColumnInFrontAndOnlyForItsOwnTotem()
    {
        var world = NewWorld();
        Put(world, A, 1, "bypasser");
        Put(world, A, 2, "ranger");
        Put(world, B, 2, "wall");

        // Slot 1 is open, so the Rogue strikes past the wall on slot 2 (§4.1);
        // a unit without Bypass on the same board cannot.
        Assert.Contains(new TotemRef(B, TotemPosition.A), CombatRules.TotemTargets(world, Catalog, A, 1));
        Assert.DoesNotContain(new TotemRef(B, TotemPosition.A), CombatRules.TotemTargets(world, Catalog, A, 2));

        Put(world, B, 1, "wall");
        Assert.Empty(CombatRules.TotemTargets(world, Catalog, A, 1));
    }

    [Fact]
    public void TheDiagonalSurchargeLimitsHowFarSidewaysATotemCanBeReached()
    {
        var world = NewWorld();
        Put(world, A, 1, "ranger");
        Put(world, A, 2, "striker");

        // Range 3 pays the surcharge down to an offset of 2: places A and B,
        // never C. Range 1 reaches only the totem straight ahead (§8.1.1).
        Assert.Equal(2, CombatRules.MaxTotemOffset(3));
        Assert.Equal(
            new[] { TotemPosition.A, TotemPosition.B },
            CombatRules.TotemTargets(world, Catalog, A, 1).Select(totem => totem.Position));
        Assert.Equal(
            new[] { TotemPosition.A },
            CombatRules.TotemTargets(world, Catalog, A, 2).Select(totem => totem.Position));
        Assert.IsType<CommandRejectedEvent>(Attack(world, 1, TotemPosition.C));
    }

    [Fact]
    public void ArcaneUnitsHaveNoTotemTargetsAtAll()
    {
        var world = NewWorld();
        Put(world, A, 1, "arcanist");

        Assert.Empty(CombatRules.TotemTargets(world, Catalog, A, 1));
        Assert.IsType<CommandRejectedEvent>(Attack(world, 1, TotemPosition.A));
    }

    [Fact]
    public void TheTotemOfLifeLosesHealthAndAtZeroTheMatchIsWon()
    {
        var world = NewWorld();
        Put(world, A, 1, "ranger");

        var attacked = Assert.IsType<UnitAttackedEvent>(Attack(world, 1, TotemPosition.A));

        var hit = Assert.Single(attacked.TotemHits);
        Assert.Equal(TotemType.Life, hit.Type);
        Assert.Equal(3, hit.Amount);
        Assert.Equal(11, world.PlayerB.Life.Health);
        Assert.Equal(world.Turn.Round, world.Turn.LastLifeDamageRound);
        Assert.Null(attacked.Outcome);

        world.PlayerB.Life.Take(10);
        Put(world, A, 2, "ranger");
        var killing = Assert.IsType<UnitAttackedEvent>(Attack(world, 2, TotemPosition.A));

        Assert.Equal(A, killing.Outcome?.Winner);
        Assert.Equal(A, world.Outcome?.Winner);
        Assert.True(world.PlayerB.Life.IsDestroyed);

        // Nothing is playable once the match is over.
        string before = WorldStateDumper.Dump(world);
        Assert.IsType<CommandRejectedEvent>(TurnSystem.Apply(world, new EndTurnCommand { Player = A }));
        Assert.IsType<CommandRejectedEvent>(Attack(world, 1, TotemPosition.B));
        Assert.Equal(before, WorldStateDumper.Dump(world));
    }

    [Fact]
    public void TheTotemOfManaLosesOneManaPerHitAndOwesItWhenItIsEmpty()
    {
        var world = NewWorld();
        Put(world, A, 1, "ranger");
        Put(world, A, 3, "twinner");
        world.PlayerB.Mana.Set(1);

        // One mana per hit, whatever the attack value; a double hit is two.
        Assert.Equal(1, Assert.IsType<UnitAttackedEvent>(Attack(world, 1, TotemPosition.B)).TotemHits[0].Amount);
        Assert.Equal(0, world.PlayerB.Mana.Current);

        Assert.Equal(2, Assert.IsType<UnitAttackedEvent>(Attack(world, 3, TotemPosition.B)).TotemHits[0].Amount);
        Assert.Equal(0, world.PlayerB.Mana.Current);
        Assert.Equal(2, world.PlayerB.Mana.Debt);

        // What is owed is missing from the next refill, once (§5.2).
        world.PlayerB.Mana.RefillTo(10);
        Assert.Equal(8, world.PlayerB.Mana.Current);
        Assert.Equal(0, world.PlayerB.Mana.Debt);
    }

    [Fact]
    public void TheTotemOfTimeLosesSecondsAndOverdamageEndsTheAttackersOwnTurn()
    {
        var world = NewWorld();
        Put(world, A, 4, "siege");
        Put(world, A, 5, "siege");
        Put(world, A, 6, "siege");

        Attack(world, 4, TotemPosition.C);
        Assert.Equal(22, world.PlayerB.Time.Seconds);
        Attack(world, 5, TotemPosition.C);
        Assert.Equal(10, world.PlayerB.Time.Seconds);

        var attacked = Assert.IsType<UnitAttackedEvent>(Attack(world, 6, TotemPosition.C));

        Assert.True(attacked.TotemHits[0].Overdamage);
        Assert.Equal(0, world.PlayerB.Time.Seconds);
        Assert.NotNull(attacked.TurnEnded);
        Assert.Equal(B, world.Turn.ActivePlayer);

        // A's own time and mana were refilled as their turn ended (G06-02).
        Assert.Equal(34, world.PlayerA.Time.Seconds);
    }

    [Fact]
    public void OnlyAKeptPatternSpreadsAlongTheTotemRow()
    {
        var world = NewWorld();
        Put(world, A, 3, "hammer");
        Put(world, A, 4, "cleaver");

        var spread = CombatRules.AffectedTotems(world, Catalog, A, 3, new TotemRef(B, TotemPosition.B));

        // The hammer comes in on column 3 and carries into column 2, which
        // belongs to place A: two totems, both at full damage (§8.6).
        Assert.Equal(new[] { TotemPosition.A, TotemPosition.B }, spread.Select(hit => hit.Totem.Position));
        Assert.All(spread, hit => Assert.Equal(2, hit.Damage));
        Assert.Equal(2, Assert.IsType<UnitAttackedEvent>(Attack(world, 3, TotemPosition.B)).TotemHits.Count);
        Assert.Equal(12, world.PlayerB.Life.Health);

        var single = CombatRules.AffectedTotems(world, Catalog, A, 4, new TotemRef(B, TotemPosition.B));
        Assert.Equal(new[] { TotemPosition.B }, single.Select(hit => hit.Totem.Position));
    }

    [Fact]
    public void EightRoundsWithoutDamageToATotemOfLifeEndInADraw()
    {
        var world = NewWorld();
        Put(world, A, 1, "ranger");

        IEvent last = null!;
        for (int turn = 0; turn < 16; turn++)
        {
            last = TurnSystem.Apply(world, new EndTurnCommand { Player = world.Turn.ActivePlayer });
        }

        var ended = Assert.IsType<MatchEndedEvent>(last);
        Assert.True(ended.Outcome.IsDraw);
        Assert.Equal(8, world.Turn.Round);
        Assert.NotNull(world.Outcome);

        // A hit on a Totem of Life sets the count back to zero.
        var fresh = NewWorld();
        Put(fresh, A, 1, "ranger");
        for (int turn = 0; turn < 8; turn++)
        {
            TurnSystem.Apply(fresh, new EndTurnCommand { Player = fresh.Turn.ActivePlayer });
        }

        Attack(fresh, 1, TotemPosition.A);
        for (int turn = 0; turn < 8; turn++)
        {
            Assert.IsType<TurnStartedEvent>(TurnSystem.Apply(fresh, new EndTurnCommand { Player = fresh.Turn.ActivePlayer }));
        }

        Assert.Null(fresh.Outcome);
    }
}
