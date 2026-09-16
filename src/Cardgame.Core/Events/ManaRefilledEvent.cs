namespace Cardgame.Core.Events;

using Cardgame.Core.Model;

public sealed record ManaRefilledEvent : IEvent
{
    public required PlayerId Player { get; init; }
    public required int Mana { get; init; }
}
