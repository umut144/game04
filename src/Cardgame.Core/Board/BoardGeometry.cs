namespace Cardgame.Core.Board;

/// <summary>
/// Where things are relative to each other on the board (GAME_DESIGN.md §3).
/// Slots and columns are numbered 1-6 on each side, and both sides count in
/// the same direction: slot n opposes slot n, and totem place A opposes A.
/// Each client mirrors its view rather than turning it, so every player sees
/// their own slot 1 on their left and the opponent's slot 1 straight across.
/// </summary>
public static class BoardGeometry
{
    public const int FirstSlot = 1;
    public const int LastSlot = BoardSide.UnitSlotCount;

    public static bool IsValidSlot(int slot) => slot is >= FirstSlot and <= LastSlot;

    /// <summary>The opponent's slot straight across from <paramref name="slot"/>.</summary>
    public static int OpposingSlot(int slot)
    {
        RequireValidSlot(slot);
        return slot;
    }

    /// <summary>The slots next to <paramref name="slot"/> on the same side, left first.</summary>
    public static IReadOnlyList<int> NeighboursOf(int slot)
    {
        RequireValidSlot(slot);
        var neighbours = new List<int>(2);
        if (slot > FirstSlot)
        {
            neighbours.Add(slot - 1);
        }

        if (slot < LastSlot)
        {
            neighbours.Add(slot + 1);
        }

        return neighbours;
    }

    /// <summary>The totem place standing behind unit column <paramref name="column"/>.</summary>
    public static TotemPosition TotemPositionBehind(int column)
    {
        RequireValidSlot(column);
        return (TotemPosition)((column - 1) / 2);
    }

    public static TotemColumnPair ColumnsOf(TotemPosition position) =>
        TotemColumnPair.StandardPairs[(int)position];

    internal static void RequireValidSlot(int slot)
    {
        if (!IsValidSlot(slot))
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot), slot, $"slots and columns run from {FirstSlot} to {LastSlot}");
        }
    }
}
