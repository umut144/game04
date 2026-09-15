namespace Cardgame.Core.Zones;

using Cardgame.Core.Model;

/// <summary>The three zones one player owns (§8.3). Board occupancy
/// is tracked on <see cref="Board.BoardState"/> instead, since it is shared
/// board geometry rather than a player-private list.</summary>
public sealed class PlayerZones
{
    public PlayerId Owner { get; }
    public Zone Deck { get; }
    public Zone Hand { get; }
    public Zone Destroyed { get; }

    public PlayerZones(PlayerId owner)
    {
        Owner = owner;
        Deck = new Zone(owner, ZoneVisibility.Hidden);
        Hand = new Zone(owner, ZoneVisibility.OwnerOnly);
        Destroyed = new Zone(owner, ZoneVisibility.Public);
    }
}
