namespace Cardgame.Core.Commands;

using Cardgame.Core.Model;

/// <summary>
/// G02 only: puts a player's mana back to its maximum so play can be tried
/// repeatedly before G04 brings the per-round refill.
/// </summary>
public sealed record RefillManaCommand : ICommand
{
    public required PlayerId Player { get; init; }
}
