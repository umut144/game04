namespace Cardgame.Core.Model;

/// <summary>
/// How a match ended (§5.1): the only way today is a Totem of Life at 0. A
/// draw has no rule — G06 tried "eight rounds without damage to a Totem of
/// Life" and the developer withdrew it — so <see cref="Winner"/> is always
/// set for now; it stays nullable because a Bomb that fells both totems at
/// once will need it (§8.4).
/// </summary>
public sealed record MatchOutcome(PlayerId? Winner, string Reason)
{
    public bool IsDraw => Winner is null;
}
