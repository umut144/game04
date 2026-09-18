namespace Cardgame.Core.Zones;

using Cardgame.Core.Clock;

/// <summary>
/// The seconds a player's next turn will have (§5.3, G06-03). The pool is
/// refilled to the maximum when its owner ends their turn, which is why it
/// stands full — and attackable — through the opponent's turn. An attack on
/// the Totem of Time takes its attack value in seconds off the pool.
/// </summary>
public sealed class TimePool
{
    public int Maximum { get; } = TurnClock.Standard.Seconds;
    public int Seconds { get; private set; }

    public void Refill() => Seconds = Maximum;

    /// <summary>
    /// Takes seconds off the pool, never below 0. True when more was taken
    /// than the pool held — the overdamage that ends the attacker's own turn.
    /// </summary>
    public bool Take(int seconds)
    {
        bool over = seconds > Seconds;
        Seconds = Math.Max(0, Seconds - seconds);
        return over;
    }
}
