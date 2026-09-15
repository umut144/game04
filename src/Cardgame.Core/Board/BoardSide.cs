namespace Cardgame.Core.Board;

using Cardgame.Core.Model;

/// <summary>
/// One side's board: 6 unit slots and 3 totem column pairs (§3). G00 only
/// carries the geometry; slot occupancy and totem identity are later gates
/// (G01, G02+).
/// </summary>
public sealed class BoardSide
{
    public const int UnitSlotCount = 6;

    public IReadOnlyList<CardInstanceId?> UnitSlots { get; }
    public IReadOnlyList<TotemColumnPair> TotemColumnPairs { get; }

    public BoardSide()
    {
        UnitSlots = new CardInstanceId?[UnitSlotCount];
        TotemColumnPairs = TotemColumnPair.StandardPairs;
    }
}
