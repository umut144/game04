namespace Cardgame.Core.Commands;

using Cardgame.Core.Model;

/// <summary>
/// Plays a unit from the player's hand onto one of their free slots at the
/// chosen tier (§7.3), paying its mana cost.
/// </summary>
public sealed record PlayUnitCommand : ICommand
{
    public required PlayerId Player { get; init; }
    public required CardInstanceId Card { get; init; }
    public required int Tier { get; init; }
    public required int Slot { get; init; }
}
