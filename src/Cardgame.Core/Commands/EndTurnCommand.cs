namespace Cardgame.Core.Commands;

using Cardgame.Core.Model;

/// <summary>Ends the active player's turn, by choice or because their time ran out.</summary>
public sealed record EndTurnCommand : ICommand
{
    public required PlayerId Player { get; init; }
    public bool TimedOut { get; init; }
}
