namespace Cardgame.Core.Events;

/// <summary>Reported once <see cref="Commands.SetupMatchCommand"/> has produced a WorldState.</summary>
public sealed record MatchSetUpEvent : IEvent
{
    public required ulong Seed { get; init; }
}
