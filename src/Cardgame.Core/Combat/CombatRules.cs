namespace Cardgame.Core.Combat;

using Cardgame.Core.Board;
using Cardgame.Core.Design;
using Cardgame.Core.Model;

/// <summary>
/// The questions combat asks of the board, answered without changing it:
/// whether a unit may attack, what it may aim at, and what an attack would
/// affect (§8.1). The client uses these for its previews, so what it shows
/// and what <see cref="Systems.CombatSystem"/> does cannot drift apart.
/// Totems are not targets before G06.
/// </summary>
public static class CombatRules
{
    /// <summary>Why the unit on the slot may not attack now, or null if it may.</summary>
    public static string? WhyCannotAttack(WorldState world, CardCatalog catalog, PlayerId player, int slot)
    {
        if (player != world.Turn.ActivePlayer)
        {
            return $"it is {world.Turn.ActivePlayer}'s turn";
        }

        if (!BoardGeometry.IsValidSlot(slot))
        {
            return $"there is no slot {slot}";
        }

        if (world.Board.Side(player).OccupantOf(slot) is not { } id)
        {
            return $"slot {slot} is empty";
        }

        var unit = world.Board.Units[id];
        if (catalog.CardsById[unit.DefinitionId].Attack is null)
        {
            return "this unit does not attack";
        }

        if (unit.HasAttacked)
        {
            return "this unit has already attacked this turn";
        }

        return unit.Ready ? null : "this unit was played this turn";
    }

    public static AttackProfile? ProfileOf(WorldState world, CardCatalog catalog, PlayerId player, int slot) =>
        world.Board.Side(player).OccupantOf(slot) is { } id
            ? catalog.CardsById[world.Board.Units[id].DefinitionId].Attack
            : null;

    /// <summary>
    /// The occupied opposing slots the unit may aim at (Range, §8.1). Empty for
    /// a pattern that takes no target.
    /// </summary>
    public static IReadOnlyList<FieldRef> Targets(WorldState world, CardCatalog catalog, PlayerId player, int slot)
    {
        var profile = ProfileOf(world, catalog, player, slot);
        if (profile is not { NeedsTarget: true, Range: int range })
        {
            return Array.Empty<FieldRef>();
        }

        var opponent = PlayerIds.Opponent(player);
        var side = world.Board.Side(opponent);
        int first = Math.Max(BoardGeometry.FirstSlot, slot - range);
        int last = Math.Min(BoardGeometry.LastSlot, slot + range);
        return Enumerable.Range(first, last - first + 1)
            .Where(side.IsOccupied)
            .Select(s => new FieldRef(opponent, s))
            .ToArray();
    }

    /// <summary>
    /// Every field the attack would affect and the damage it would deal there,
    /// empty fields included, as the preview shows it (§8.2).
    /// </summary>
    public static IReadOnlyList<(FieldRef Field, int Damage)> AffectedFields(
        WorldState world, CardCatalog catalog, PlayerId player, int slot, FieldRef? target)
    {
        if (world.Board.Side(player).OccupantOf(slot) is not { } id)
        {
            return Array.Empty<(FieldRef, int)>();
        }

        var unit = world.Board.Units[id];
        var definition = catalog.CardsById[unit.DefinitionId];
        if (definition.Attack is not { } profile)
        {
            return Array.Empty<(FieldRef, int)>();
        }

        int attack = definition.Tiers[(unit.Tier ?? 1) - 1].Attack;
        var fields = new List<(FieldRef, int)>();
        if (profile.Pattern == AttackPattern.BothFrontRows)
        {
            foreach (var side in new[] { player, PlayerIds.Opponent(player) })
            {
                for (int s = BoardGeometry.FirstSlot; s <= BoardGeometry.LastSlot; s++)
                {
                    if (side != player || s != slot)
                    {
                        fields.Add((new FieldRef(side, s), attack));
                    }
                }
            }

            return fields;
        }

        if (target is not { } aim)
        {
            return fields;
        }

        int beside = profile.Pattern switch
        {
            AttackPattern.AreaThree or AttackPattern.Splash => attack,
            AttackPattern.Cleave => attack / 2,
            _ => 0,
        };
        int onTarget = profile.Pattern == AttackPattern.DoubleHit ? attack * 2 : attack;

        fields.Add((aim, onTarget));
        if (beside > 0)
        {
            foreach (int s in BoardGeometry.NeighboursOf(aim.Slot))
            {
                fields.Add((new FieldRef(aim.Side, s), beside));
            }
        }

        return fields;
    }

    public static int CurrentHealth(CardCatalog catalog, CardInstance unit) =>
        catalog.CardsById[unit.DefinitionId].Tiers[(unit.Tier ?? 1) - 1].Health - unit.Damage;
}
