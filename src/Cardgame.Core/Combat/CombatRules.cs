namespace Cardgame.Core.Combat;

using Cardgame.Core.Board;
using Cardgame.Core.Design;
using Cardgame.Core.Model;

/// <summary>
/// The questions combat asks of the board, answered without changing it:
/// whether a unit may attack, what it may aim at, and what an attack would
/// affect (§8.1). The client uses these for its previews, so what it shows
/// and what <see cref="Systems.CombatSystem"/> does cannot drift apart.
/// Totems became targets in G06 (§4, §8.1.1, §8.6).
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

    /// <summary>
    /// The largest lateral offset at which a totem column is still reachable
    /// with this Range: floor(2R/3), the closed form of the diagonal
    /// surcharge (§8.1.1).
    /// </summary>
    public static int MaxTotemOffset(int range) => 2 * range / 3;

    /// <summary>
    /// The opposing totem places the unit on the slot may attack: in reach
    /// after the surcharge, and not protected by a unit in either of the
    /// totem's two columns (§4). A unit with Bypass needs only the column
    /// directly in front of itself to be open, and only for the totem that
    /// column belongs to (§4.1). Arcane units cannot damage totems at all
    /// (§7.6), so they never have one.
    /// </summary>
    public static IReadOnlyList<TotemRef> TotemTargets(WorldState world, CardCatalog catalog, PlayerId player, int slot)
    {
        if (!BoardGeometry.IsValidSlot(slot) || world.Board.Side(player).OccupantOf(slot) is not { } id)
        {
            return Array.Empty<TotemRef>();
        }

        var unit = world.Board.Units[id];
        var definition = catalog.CardsById[unit.DefinitionId];
        if (definition.Type == CardType.Arcane || definition.Attack is not { NeedsTarget: true, Range: int range })
        {
            return Array.Empty<TotemRef>();
        }

        var opponent = PlayerIds.Opponent(player);
        var side = world.Board.Side(opponent);
        int reach = MaxTotemOffset(range);
        int? bypassColumn = definition.Has(Abilities.Bypass, unit.Tier ?? 1) ? slot : null;

        var targets = new List<TotemRef>();
        for (int index = 0; index < TotemColumnPair.StandardPairs.Count; index++)
        {
            var pair = TotemColumnPair.StandardPairs[index];
            int offset = Math.Min(Math.Abs(pair.FirstColumn - slot), Math.Abs(pair.SecondColumn - slot));
            if (offset > reach || IsProtected(side, pair, bypassColumn))
            {
                continue;
            }

            targets.Add(new TotemRef(opponent, (TotemPosition)index));
        }

        return targets;
    }

    /// <summary>Whether a unit in the totem's columns shields it (§4).</summary>
    public static bool IsTotemProtected(WorldState world, TotemRef totem) =>
        IsProtected(world.Board.Side(totem.Side), totem.Columns, null);

    private static bool IsProtected(BoardSide side, TotemColumnPair pair, int? bypassColumn)
    {
        if (bypassColumn is int column
            && (pair.FirstColumn == column || pair.SecondColumn == column)
            && !side.IsOccupied(column))
        {
            return false;
        }

        return side.IsOccupied(pair.FirstColumn) || side.IsOccupied(pair.SecondColumn);
    }

    /// <summary>
    /// The totems an attack aimed at <paramref name="aim"/> would hit, with
    /// the damage each takes and how many separate hits it is — a double hit
    /// strikes twice, which matters to a Totem of Mana, where every hit costs
    /// exactly one mana whatever the attack value (§8.6). A pattern only
    /// spreads along the totem row when the card keeps it there
    /// (<see cref="TotemPattern.Keep"/>); the spread pays no surcharge (§8.1.1).
    /// </summary>
    public static IReadOnlyList<(TotemRef Totem, int Damage, int Hits)> AffectedTotems(
        WorldState world, CardCatalog catalog, PlayerId player, int slot, TotemRef aim)
    {
        if (world.Board.Side(player).OccupantOf(slot) is not { } id)
        {
            return Array.Empty<(TotemRef, int, int)>();
        }

        var unit = world.Board.Units[id];
        var definition = catalog.CardsById[unit.DefinitionId];
        if (definition.Attack is not { } profile)
        {
            return Array.Empty<(TotemRef, int, int)>();
        }

        int attack = definition.Tiers[(unit.Tier ?? 1) - 1].Attack;
        int hits = profile.Pattern == AttackPattern.DoubleHit ? 2 : 1;
        if (profile.TotemPattern == TotemPattern.Single)
        {
            return new[] { (aim, attack, hits) };
        }

        int beside = profile.Pattern switch
        {
            AttackPattern.AreaThree or AttackPattern.Splash => attack,
            AttackPattern.Cleave => attack / 2,
            _ => 0,
        };

        // The column the attack comes in on is the aimed totem's column
        // nearest the attacker; the pattern then spreads from there.
        var pair = aim.Columns;
        int aimColumn = Math.Abs(pair.FirstColumn - slot) <= Math.Abs(pair.SecondColumn - slot)
            ? pair.FirstColumn
            : pair.SecondColumn;

        var damageByPosition = new Dictionary<TotemPosition, int> { [aim.Position] = attack };
        if (beside > 0)
        {
            foreach (int column in BoardGeometry.NeighboursOf(aimColumn))
            {
                var position = PositionOfColumn(column);
                damageByPosition[position] = Math.Max(damageByPosition.GetValueOrDefault(position), beside);
            }
        }

        return damageByPosition
            .OrderBy(entry => entry.Key)
            .Select(entry => (new TotemRef(aim.Side, entry.Key), entry.Value, hits))
            .ToArray();
    }

    private static TotemPosition PositionOfColumn(int column)
    {
        for (int index = 0; index < TotemColumnPair.StandardPairs.Count; index++)
        {
            var pair = TotemColumnPair.StandardPairs[index];
            if (pair.FirstColumn == column || pair.SecondColumn == column)
            {
                return (TotemPosition)index;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(column), column, "no totem stands behind this column");
    }

    public static int CurrentHealth(CardCatalog catalog, CardInstance unit) =>
        catalog.CardsById[unit.DefinitionId].Tiers[(unit.Tier ?? 1) - 1].Health - unit.Damage;
}
