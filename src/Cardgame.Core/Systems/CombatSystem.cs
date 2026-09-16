namespace Cardgame.Core.Systems;

using Cardgame.Core.Board;
using Cardgame.Core.Combat;
using Cardgame.Core.Commands;
using Cardgame.Core.Design;
using Cardgame.Core.Events;
using Cardgame.Core.Model;

/// <summary>
/// Unit combat (G05). A ready unit attacks once per turn; there is no
/// counter-damage. Damage stays on a unit; at 0 health it is destroyed —
/// the one way off the board (§8.3) — goes to its owner's destroyed list,
/// and its tier's Bounty goes to its owner's opponent as Coins (§10), even
/// when its own side destroyed it. An Arcane attacker refunds 1 mana to its
/// controller when the attack destroys at least one enemy unit (§7.6).
/// </summary>
public static class CombatSystem
{
    public static IEvent Apply(WorldState world, CardCatalog catalog, AttackCommand command)
    {
        if (CombatRules.WhyCannotAttack(world, catalog, command.Player, command.AttackerSlot) is { } reason)
        {
            return Rejected(command, reason);
        }

        var attackerId = world.Board.Side(command.Player).OccupantOf(command.AttackerSlot)!.Value;
        var attacker = world.Board.Units[attackerId];
        var definition = catalog.CardsById[attacker.DefinitionId];
        var profile = definition.Attack!;

        if (profile.NeedsTarget)
        {
            if (command.Target is not { } target)
            {
                return Rejected(command, "this attack needs a target");
            }

            if (!CombatRules.Targets(world, catalog, command.Player, command.AttackerSlot).Contains(target))
            {
                return Rejected(command, $"{target} is not a target in reach");
            }
        }

        var affected = CombatRules.AffectedFields(
            world, catalog, command.Player, command.AttackerSlot, profile.NeedsTarget ? command.Target : null);

        // Double hit: the second blow is only struck if the first did not kill.
        var damageByCard = new Dictionary<CardInstanceId, (FieldRef Field, int Damage)>();
        foreach (var (field, damage) in affected)
        {
            if (world.Board.Side(field.Side).OccupantOf(field.Slot) is not { } id || damage <= 0)
            {
                continue;
            }

            var unit = world.Board.Units[id];
            int dealt = damage;
            if (profile.Pattern == AttackPattern.DoubleHit)
            {
                int single = damage / 2;
                dealt = CombatRules.CurrentHealth(catalog, unit) <= single ? single : damage;
            }

            damageByCard[id] = (field, dealt);
        }

        attacker.HasAttacked = true;
        var hits = new List<Hit>();
        int enemyKills = 0;
        foreach (var (id, (field, dealt)) in damageByCard)
        {
            var unit = world.Board.Units[id];
            unit.Damage += dealt;
            bool destroyed = CombatRules.CurrentHealth(catalog, unit) <= 0;
            int bounty = 0;
            if (destroyed)
            {
                bounty = Destroy(world, catalog, field, unit);
                if (field.Side != command.Player)
                {
                    enemyKills++;
                }
            }

            hits.Add(new Hit(field, id, dealt, destroyed, bounty));
        }

        int refund = 0;
        if (definition.Type == CardType.Arcane && enemyKills > 0)
        {
            var mana = world.Zones(command.Player).Mana;
            int before = mana.Current;
            mana.Gain(1);
            refund = mana.Current - before;
        }

        return new UnitAttackedEvent
        {
            Player = command.Player,
            AttackerSlot = command.AttackerSlot,
            Attacker = attackerId,
            Target = profile.NeedsTarget ? command.Target : null,
            Hits = hits,
            ManaRefunded = refund,
        };
    }

    private static int Destroy(WorldState world, CardCatalog catalog, FieldRef field, CardInstance unit)
    {
        int bounty = catalog.CardsById[unit.DefinitionId].Tiers[(unit.Tier ?? 1) - 1].Bounty;
        world.Board.Side(field.Side).Vacate(field.Slot);
        world.Board.Remove(unit);
        world.Zones(field.Side).Destroyed.Add(unit);
        world.Zones(PlayerIds.Opponent(field.Side)).Coins += bounty;
        return bounty;
    }

    private static CommandRejectedEvent Rejected(ICommand command, string reason) =>
        new() { Command = command, Reason = reason };
}
