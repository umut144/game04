namespace Cardgame.Core.Zones;

using Cardgame.Core.Model;

/// <summary>The three zones one player owns (§8.3), and their mana (§6). Board occupancy
/// is tracked on <see cref="Board.BoardState"/> instead, since it is shared
/// board geometry rather than a player-private list.</summary>
public sealed class PlayerZones
{
    public PlayerId Owner { get; }
    public Zone Deck { get; }
    public Zone Hand { get; }
    public Zone Destroyed { get; }
    public ManaPool Mana { get; } = new();

    /// <summary>The owner's Totem of Life (§5.1) and Totem of Time (§5.3).</summary>
    public LifeTotem Life { get; } = new();

    public TimePool Time { get; } = new();

    /// <summary>Coins are kept across rounds (§10).</summary>
    public int Coins { get; internal set; }

    public const int HandLimit = 8;

    public PlayerZones(PlayerId owner)
    {
        Owner = owner;
        Deck = new Zone(owner, ZoneVisibility.Hidden);
        Hand = new Zone(owner, ZoneVisibility.OwnerOnly);
        Destroyed = new Zone(owner, ZoneVisibility.Public);
    }
}
