namespace Cardgame.Core.Events;

using Cardgame.Core.Model;

public sealed record UnitPlayedEvent : IEvent
{
    public required PlayerId Player { get; init; }
    public required CardInstanceId Card { get; init; }
    public required int Tier { get; init; }
    public required int Slot { get; init; }
    public required int ManaPaid { get; init; }
}
