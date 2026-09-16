namespace Cardgame.Core.Events;

using Cardgame.Core.Model;

/// <summary>
/// A player's turn began: their mana was set and, if their hand had room and
/// their deck a card, they drew one.
/// </summary>
public sealed record TurnStartedEvent : IEvent
{
    public required PlayerId Player { get; init; }
    public required int Round { get; init; }
    public required int Mana { get; init; }
    public CardInstanceId? Drawn { get; init; }

    /// <summary>The turn that ended to start this one; null for the first turn.</summary>
    public PlayerId? Previous { get; init; }
    public bool PreviousTimedOut { get; init; }
}
