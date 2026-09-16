namespace Cardgame.Core.Systems;

using Cardgame.Core.Board;
using Cardgame.Core.Commands;
using Cardgame.Core.Design;
using Cardgame.Core.Events;
using Cardgame.Core.Model;

/// <summary>
/// Playing a unit from hand (G02): the card must be in the player's hand,
/// the slot on their side free, the tier 1-3 and its cost affordable. Bad
/// input is answered with <see cref="CommandRejectedEvent"/>; the world is
/// then unchanged.
/// </summary>
public static class CardPlaySystem
{
    public static IEvent Apply(WorldState world, CardCatalog catalog, PlayUnitCommand command)
    {
        if (command.Player != world.Turn.ActivePlayer)
        {
            return Rejected(command, $"it is {world.Turn.ActivePlayer}'s turn");
        }

        var zones = world.Zones(command.Player);
        var card = zones.Hand.Find(command.Card);
        if (card is null)
        {
            return Rejected(command, $"{command.Card} is not in {command.Player}'s hand");
        }

        if (command.Tier is < 1 or > 3)
        {
            return Rejected(command, $"there is no tier {command.Tier}");
        }

        if (!BoardGeometry.IsValidSlot(command.Slot))
        {
            return Rejected(command, $"there is no slot {command.Slot}");
        }

        var side = world.Board.Side(command.Player);
        if (side.IsOccupied(command.Slot))
        {
            return Rejected(command, $"slot {command.Slot} is already occupied");
        }

        int cost = catalog.CardsById[card.DefinitionId].Tiers[command.Tier - 1].Cost;
        if (!zones.Mana.CanPay(cost))
        {
            return Rejected(command, $"tier {command.Tier} costs {cost} mana, {zones.Mana.Current} available");
        }

        zones.Mana.Pay(cost);
        zones.Hand.Remove(card);
        PlaceOnBoard(world, command.Player, command.Slot, card, command.Tier,
            catalog.CardsById[card.DefinitionId].Has(Abilities.Rush, command.Tier));

        return new UnitPlayedEvent
        {
            Player = command.Player,
            Card = card.Id,
            Tier = command.Tier,
            Slot = command.Slot,
            ManaPaid = cost,
        };
    }

    internal static void PlaceOnBoard(WorldState world, PlayerId player, int slot, CardInstance card, int tier, bool ready)
    {
        card.Tier = tier;
        card.Damage = 0;
        card.Ready = ready;
        card.HasAttacked = false;
        world.Board.Side(player).Occupy(slot, card.Id);
        world.Board.Place(card);
    }

    public static IEvent Apply(WorldState world, RefillManaCommand command)
    {
        var mana = world.Zones(command.Player).Mana;
        mana.Refill();
        return new ManaRefilledEvent { Player = command.Player, Mana = mana.Current };
    }

    private static CommandRejectedEvent Rejected(ICommand command, string reason) =>
        new() { Command = command, Reason = reason };
}
