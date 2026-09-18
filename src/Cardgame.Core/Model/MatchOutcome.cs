namespace Cardgame.Core.Model;

/// <summary>
/// How a match ended (§5.1, G06-05): a win, or a draw after
/// <see cref="Systems.TurnSystem.DrawAfterQuietRounds"/> rounds in which no
/// Totem of Life took damage. <see cref="Winner"/> is null for a draw.
/// </summary>
public sealed record MatchOutcome(PlayerId? Winner, string Reason)
{
    public bool IsDraw => Winner is null;
}
