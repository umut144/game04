namespace Cardgame.Core.Events;

using Cardgame.Core.Model;

public sealed record BlankCardPlacedEvent : IEvent
{
    public required PlayerId Player { get; init; }
    public required int Slot { get; init; }
    public required CardInstanceId Card { get; init; }
}
