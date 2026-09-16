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

public sealed class CombatSystemTests
{
    private static readonly CardCatalog Catalog = CombatCards.BuildCatalog();

    // An empty board, A to move, both at 7 mana.
    private static WorldState NewWorld()
    {
        WorldState world;
        ulong seed = 0;
        do
        {
            world = MatchSetupSystem.Apply(
                new SetupMatchCommand
                {
                    Seed = seed++,
                    MatchMode = MatchMode.ShuffledMirror,
                    PlayerADeckDefinitionIds = Array.Empty<string>(),
                    PlayerBDeckDefinitionIds = Array.Empty<string>(),
                },
                Catalog).World;
        }
        while (world.Turn.ActivePlayer != PlayerId.PlayerA);

        return world;
    }

    private static CardInstance Put(WorldState world, PlayerId side, int slot, string id, bool ready = true)
    {
        var card = new CardInstance(world.CardIds.Next(), id);
        CardPlaySystem.PlaceOnBoard(world, side, slot, card, 1, ready);
        return card;
    }

    private static IEvent Attack(WorldState world, int from, int? to) =>
        CombatSystem.Apply(world, Catalog, new AttackCommand
        {
            Player = PlayerId.PlayerA,
            AttackerSlot = from,
            Target = to is int slot ? new FieldRef(PlayerId.PlayerB, slot) : null,
        });

    private const PlayerId A = PlayerId.PlayerA;
    private const PlayerId B = PlayerId.PlayerB;

    [Fact]
    public void RangeReachesTheOpposingSlotsWithinTheOffset()
    {
        var world = NewWorld();
        Put(world, A, 1, "striker");
        foreach (int s in new[] { 1, 2, 3 })
        {
            Put(world, B, s, "wall");
        }

        Assert.Equal(
            new[] { new FieldRef(B, 1), new FieldRef(B, 2) },
            CombatRules.Targets(world, Catalog, A, 1));
        Assert.IsType<CommandRejectedEvent>(Attack(world, 1, 3));
    }

    [Fact]
    public void DamageStaysAndDestructionPaysTheBountyToTheOpponent()
    {
        var world = NewWorld();
        Put(world, A, 2, "striker");
        var target = Put(world, B, 2, "wall");

        var first = Assert.IsType<UnitAttackedEvent>(Attack(world, 2, 2));
        Assert.Equal(new Hit(new FieldRef(B, 2), target.Id, 3, true, 3), Assert.Single(first.Hits));
        Assert.False(world.Board.PlayerB.IsOccupied(2));
        Assert.Contains(target, world.PlayerB.Destroyed.Cards);
        Assert.Equal(3, world.PlayerA.Coins);
        Assert.Equal(0, world.PlayerB.Coins);

        var tough = Put(world, B, 3, "tough");
        TurnSystem.Apply(world, new EndTurnCommand { Player = A });
        TurnSystem.Apply(world, new EndTurnCommand { Player = B });
        Attack(world, 2, 3);
        TurnSystem.Apply(world, new EndTurnCommand { Player = A });
        TurnSystem.Apply(world, new EndTurnCommand { Player = B });
        Attack(world, 2, 3);
        Assert.Equal(6, tough.Damage);
        Assert.Equal(3, CombatRules.CurrentHealth(Catalog, tough));
    }

    [Fact]
    public void AUnitAttacksOncePerTurnAndNotInTheTurnItWasPlayedUnlessItHasRush()
    {
        var world = NewWorld();
        Put(world, A, 1, "striker", ready: false);
        Put(world, B, 1, "tough");
        Assert.Contains("played this turn", Assert.IsType<CommandRejectedEvent>(Attack(world, 1, 1)).Reason);

        var hand = world.PlayerA.Hand;
        var rusher = new CardInstance(world.CardIds.Next(), "striker");
        hand.Add(rusher);
        Assert.IsType<UnitPlayedEvent>(CardPlaySystem.Apply(world, Catalog,
            new PlayUnitCommand { Player = A, Card = rusher.Id, Tier = 3, Slot = 2 }));
        Assert.IsType<UnitAttackedEvent>(Attack(world, 2, 1));
        Assert.Contains("already attacked", Assert.IsType<CommandRejectedEvent>(Attack(world, 2, 1)).Reason);

        TurnSystem.Apply(world, new EndTurnCommand { Player = A });
        Assert.IsType<CommandRejectedEvent>(CombatSystem.Apply(world, Catalog,
            new AttackCommand { Player = A, AttackerSlot = 1, Target = new FieldRef(B, 1) }));
        TurnSystem.Apply(world, new EndTurnCommand { Player = B });
        Assert.IsType<UnitAttackedEvent>(Attack(world, 1, 1));
        Assert.IsType<UnitAttackedEvent>(Attack(world, 2, 1));
    }

    [Fact]
    public void CleaveHitsTheNeighboursForHalfRoundedDown()
    {
        var world = NewWorld();
        Put(world, A, 3, "cleaver");
        var left = Put(world, B, 2, "tough");
        var middle = Put(world, B, 3, "tough");
        var right = Put(world, B, 4, "tough");

        Assert.IsType<UnitAttackedEvent>(Attack(world, 3, 3));

        Assert.Equal((1, 3, 1), (left.Damage, middle.Damage, right.Damage));
    }

    [Fact]
    public void SplashAndAreaHitTheTargetAndBothNeighboursInFull()
    {
        var world = NewWorld();
        Put(world, A, 1, "splasher");
        Put(world, A, 4, "area");
        var one = Put(world, B, 1, "tough");
        var two = Put(world, B, 2, "tough");
        var five = Put(world, B, 5, "tough");

        Attack(world, 1, 1);
        Assert.Equal((2, 2), (one.Damage, two.Damage));
        Assert.Equal(
            new[] { (new FieldRef(B, 1), 2), (new FieldRef(B, 2), 2) },
            CombatRules.AffectedFields(world, Catalog, A, 1, new FieldRef(B, 1)));

        Attack(world, 4, 5);
        Assert.Equal(2, five.Damage);
        Assert.Equal(
            new[] { new FieldRef(B, 5), new FieldRef(B, 4), new FieldRef(B, 6) },
            CombatRules.AffectedFields(world, Catalog, A, 4, new FieldRef(B, 5)).Select(f => f.Field));
    }

    [Fact]
    public void ADoubleHitStrikesTwiceUnlessTheFirstBlowKills()
    {
        var world = NewWorld();
        Put(world, A, 1, "doubler");
        Put(world, A, 2, "doubler");
        var tough = Put(world, B, 1, "tough");
        var wall = Put(world, B, 2, "wall");
        wall.Damage = 1;

        Attack(world, 1, 1);
        var second = Assert.IsType<UnitAttackedEvent>(Attack(world, 2, 2));

        Assert.Equal(4, tough.Damage);
        Assert.Equal(2, Assert.Single(second.Hits).Damage);
        Assert.True(second.Hits[0].Destroyed);
    }

    [Fact]
    public void BothFrontRowsHitsEveryoneButTheAttackerAndOwnLossesStillPayTheOpponent()
    {
        var world = NewWorld();
        Put(world, A, 3, "storm");
        var own = Put(world, A, 4, "wall");
        own.Damage = 2;
        var enemy = Put(world, B, 6, "tough");

        var attacked = Assert.IsType<UnitAttackedEvent>(Attack(world, 3, null));

        Assert.Equal(2, attacked.Hits.Count);
        Assert.Equal(1, enemy.Damage);
        Assert.Contains(own, world.PlayerA.Destroyed.Cards);
        Assert.Equal(3, world.PlayerB.Coins);
        Assert.Equal(11, CombatRules.AffectedFields(world, Catalog, A, 3, null).Count);
        Assert.Equal(0, attacked.ManaRefunded);
        Assert.Empty(CombatRules.Targets(world, Catalog, A, 3));
    }

    [Fact]
    public void AnArcaneKillRefundsOneMana()
    {
        var world = NewWorld();
        Put(world, A, 1, "arcanist");
        Put(world, A, 2, "striker");
        Put(world, B, 1, "wall");
        Put(world, B, 2, "tough");
        world.PlayerA.Mana.Set(4);

        Assert.Equal(1, Assert.IsType<UnitAttackedEvent>(Attack(world, 1, 1)).ManaRefunded);
        Assert.Equal(5, world.PlayerA.Mana.Current);

        Attack(world, 2, 2);
        Assert.Equal(5, world.PlayerA.Mana.Current);
    }

    [Fact]
    public void BadAttacksAreRejectedAndChangeNothing()
    {
        var world = NewWorld();
        Put(world, A, 1, "striker");
        Put(world, A, 2, "wall");
        Put(world, B, 1, "wall");
        string before = WorldStateDumper.Dump(world);

        var bad = new IEvent[]
        {
            Attack(world, 3, 1),
            Attack(world, 2, 1),
            Attack(world, 1, null),
            Attack(world, 1, 2),
            Attack(world, 0, 1),
            CombatSystem.Apply(world, Catalog, new AttackCommand { Player = A, AttackerSlot = 1, Target = new FieldRef(A, 2) }),
            CombatSystem.Apply(world, Catalog, new AttackCommand { Player = B, AttackerSlot = 1, Target = new FieldRef(A, 1) }),
        };

        Assert.All(bad, result => Assert.IsType<CommandRejectedEvent>(result));
        Assert.Equal(before, WorldStateDumper.Dump(world));
    }
}
