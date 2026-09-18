namespace Cardgame.Core.Events;

using Cardgame.Core.Model;

/// <summary>The match is over: a Totem of Life fell, or the rounds ran quiet (§5.1).</summary>
public sealed record MatchEndedEvent : IEvent
{
    public required MatchOutcome Outcome { get; init; }
}
