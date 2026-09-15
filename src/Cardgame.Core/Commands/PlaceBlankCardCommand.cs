namespace Cardgame.Core.Commands;

using Cardgame.Core.Model;

/// <summary>
/// G01 only: puts a new, blank card on a free slot, without hand or cost, so
/// slot occupancy can be exercised and seen before cards exist. G02's real
/// card play replaces it.
/// </summary>
public sealed record PlaceBlankCardCommand : ICommand
{
    public required PlayerId Player { get; init; }
    public required int Slot { get; init; }
}
