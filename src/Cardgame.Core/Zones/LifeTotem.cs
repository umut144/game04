namespace Cardgame.Core.Zones;

/// <summary>
/// A player's Totem of Life (§5.1): 14 segments, one health step each, and
/// the match's win condition — at 0 the owner has lost. Damage stays until
/// it is healed (G07).
/// </summary>
public sealed class LifeTotem
{
    /// <summary>One per segment of the authored totem (SYNC-02).</summary>
    public const int StandardMaximum = 14;

    public int Maximum { get; } = StandardMaximum;
    public int Damage { get; private set; }
    public int Health => Maximum - Damage;
    public bool IsDestroyed => Health <= 0;

    public void Take(int damage) => Damage = Math.Clamp(Damage + damage, 0, Maximum);
}
