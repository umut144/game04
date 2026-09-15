namespace Cardgame.Core.Commands;

using Cardgame.Core.Model;

/// <summary>G01 only: takes whatever stands on a slot off the board again.</summary>
public sealed record ClearSlotCommand : ICommand
{
    public required PlayerId Player { get; init; }
    public required int Slot { get; init; }
}
