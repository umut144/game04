namespace Cardgame.Core.Clock;

/// <summary>
/// The turn clock's logical shape only (CORE-01): a turn is 34 seconds, in
/// one piece (§5.3, G06-03). Core never reads a wall clock; whoever keeps
/// real time — the server, or the client's dev bootstrap until one exists —
/// ends the turn with an <c>EndTurnCommand</c> once the seconds are spent.
/// How many seconds a given turn actually has is its player's
/// <see cref="Zones.TimePool"/>, which the Totem of Time's damage shortens.
/// </summary>
public sealed record TurnClock
{
    public required int Seconds { get; init; }

    /// <summary>34 s per turn, no split into base and bonus (G06-03).</summary>
    public static TurnClock Standard { get; } = new() { Seconds = 34 };
}
