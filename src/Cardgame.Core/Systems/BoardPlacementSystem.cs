namespace Cardgame.Core.Systems;

using Cardgame.Core.Board;
using Cardgame.Core.Commands;
using Cardgame.Core.Events;

/// <summary>
/// G01's slot occupancy commands. The blank card has no definition and lives
/// only as an id on the board; G02 gives played cards a proper home.
/// </summary>
public static class BoardPlacementSystem
{
    public static IEvent Apply(WorldState world, PlaceBlankCardCommand command)
    {
        if (!BoardGeometry.IsValidSlot(command.Slot))
        {
            return Rejected(command, $"there is no slot {command.Slot}");
        }

        var side = world.Board.Side(command.Player);
        if (side.IsOccupied(command.Slot))
        {
            return Rejected(command, $"slot {command.Slot} is already occupied");
        }

        var card = world.CardIds.Next();
        side.Occupy(command.Slot, card);
        return new BlankCardPlacedEvent { Player = command.Player, Slot = command.Slot, Card = card };
    }

    public static IEvent Apply(WorldState world, ClearSlotCommand command)
    {
        if (!BoardGeometry.IsValidSlot(command.Slot))
        {
            return Rejected(command, $"there is no slot {command.Slot}");
        }

        var side = world.Board.Side(command.Player);
        if (!side.IsOccupied(command.Slot))
        {
            return Rejected(command, $"slot {command.Slot} is empty");
        }

        var card = side.Vacate(command.Slot);
        return new SlotClearedEvent { Player = command.Player, Slot = command.Slot, Card = card };
    }

    private static CommandRejectedEvent Rejected(ICommand command, string reason) =>
        new() { Command = command, Reason = reason };
}
