namespace Cardgame.Core.Snapshot;

using Cardgame.Core.Board;
using Cardgame.Core.Model;

/// <summary>
/// A copy of one side's board as it was when projected. The board is public
/// information, so both sides are shown in full.
/// </summary>
public sealed record BoardSideView
{
    /// <summary>Slot occupants, index 0 being slot 1.</summary>
    public required IReadOnlyList<CardInstanceId?> UnitSlots { get; init; }

    /// <summary>The totem on each place, ordered A, B, C.</summary>
    public required IReadOnlyList<TotemPlacement> Totems { get; init; }

    public static BoardSideView CopyOf(BoardSide side) => new()
    {
        UnitSlots = side.UnitSlots.ToArray(),
        Totems = side.Totems.ToArray(),
    };
}
