namespace Cardgame.Core.Events;

using Cardgame.Core.Commands;

/// <summary>
/// A command the rules do not allow. The world is unchanged; a player's bad
/// input is an answer, never an exception.
/// </summary>
public sealed record CommandRejectedEvent : IEvent
{
    public required ICommand Command { get; init; }
    public required string Reason { get; init; }
}
