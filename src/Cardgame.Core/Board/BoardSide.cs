namespace Cardgame.Core.Board;

using Cardgame.Core.Model;

/// <summary>
/// One side's board (§3): 6 unit slots and 3 totem places. Slots are
/// addressed 1-6 (<see cref="BoardGeometry"/>). The methods here throw on
/// misuse; a player's command is checked by its system first and rejected
/// with an event instead.
/// </summary>
public sealed class BoardSide
{
    public const int UnitSlotCount = 6;

    private readonly CardInstanceId?[] _slots = new CardInstanceId?[UnitSlotCount];
    private TotemPlacement[] _totems = Array.Empty<TotemPlacement>();

    /// <summary>Slot occupants, index 0 being slot 1.</summary>
    public IReadOnlyList<CardInstanceId?> UnitSlots => _slots;

    public IReadOnlyList<TotemColumnPair> TotemColumnPairs => TotemColumnPair.StandardPairs;

    /// <summary>
    /// The totem on each place, ordered A, B, C. Empty until match setup has
    /// rolled the layout.
    /// </summary>
    public IReadOnlyList<TotemPlacement> Totems => _totems;

    public CardInstanceId? OccupantOf(int slot)
    {
        BoardGeometry.RequireValidSlot(slot);
        return _slots[slot - 1];
    }

    public bool IsOccupied(int slot) => OccupantOf(slot).HasValue;

    public void Occupy(int slot, CardInstanceId card)
    {
        if (IsOccupied(slot))
        {
            throw new InvalidOperationException($"slot {slot} is already occupied");
        }

        _slots[slot - 1] = card;
    }

    public CardInstanceId Vacate(int slot)
    {
        var occupant = OccupantOf(slot)
            ?? throw new InvalidOperationException($"slot {slot} is empty");
        _slots[slot - 1] = null;
        return occupant;
    }

    public TotemType TotemAt(TotemPosition position)
    {
        if (_totems.Length == 0)
        {
            throw new InvalidOperationException("the totem layout has not been set");
        }

        return _totems[(int)position].Type;
    }

    public TotemPosition PositionOf(TotemType type)
    {
        foreach (var placement in _totems)
        {
            if (placement.Type == type)
            {
                return placement.Position;
            }
        }

        throw new InvalidOperationException("the totem layout has not been set");
    }

    /// <summary>
    /// Sets this side's layout: <paramref name="typesByPosition"/>[0] stands
    /// on A, [1] on B, [2] on C. Every type exactly once. Each side keeps its
    /// own copy, so one side can later deviate from a mirrored layout (the
    /// Stamina 3 rule, GAME_DESIGN.md §2) without touching the other.
    /// </summary>
    public void SetTotemLayout(IReadOnlyList<TotemType> typesByPosition)
    {
        var all = Enum.GetValues<TotemType>();
        if (typesByPosition.Count != all.Length || typesByPosition.Distinct().Count() != all.Length)
        {
            throw new ArgumentException("a totem layout names each totem exactly once", nameof(typesByPosition));
        }

        _totems = typesByPosition
            .Select((type, index) => new TotemPlacement((TotemPosition)index, type))
            .ToArray();
    }
}
