namespace Cardgame.Core.Clock;

/// <summary>
/// The turn clock's logical shape only (CORE-01). Real wall-clock deadlines,
/// the scheduler that enforces them and the production of a "time's up"
/// event all live in Cardgame.Server - Core never reads a wall clock, so it
/// stays deterministic and unit-testable. The concrete seconds (§5.3) and
/// the round loop that consumes this arrive with G04; nothing in G00
/// constructs one yet.
/// </summary>
public sealed record TurnClock
{
    public required int BaseSeconds { get; init; }
    public required int BonusSeconds { get; init; }
}
