namespace Cardgame.Core.Clock;

/// <summary>
/// The turn clock's logical shape only (CORE-01): a turn has base time and
/// then bonus time (§5.3). Core never reads a wall clock; whoever keeps real
/// time — the server, or the client's dev bootstrap until one exists — ends
/// the turn with an <c>EndTurnCommand</c> once both have run out.
/// </summary>
public sealed record TurnClock
{
    public required int BaseSeconds { get; init; }
    public required int BonusSeconds { get; init; }

    /// <summary>20 s base, 10 s bonus (G04-04).</summary>
    public static TurnClock Standard { get; } = new() { BaseSeconds = 20, BonusSeconds = 10 };

    public int TotalSeconds => BaseSeconds + BonusSeconds;
}
