namespace Cardgame.Core;

using Cardgame.Core.Board;
using Cardgame.Core.Clock;
using Cardgame.Core.Model;
using Cardgame.Core.Turns;
using Cardgame.Core.Zones;

/// <summary>
/// The whole state of one match (§3, §6, §7): board, zones and the match
/// seed. Everything that reads or writes this state beyond setup arrives
/// gate by gate.
/// </summary>
public sealed class WorldState
{
    public ulong Seed { get; }
    public MatchMode MatchMode { get; }
    public BoardState Board { get; }
    public PlayerZones PlayerA { get; }
    public PlayerZones PlayerB { get; }
    public TurnState Turn { get; } = new();
    public TurnClock Clock { get; } = TurnClock.Standard;

    /// <summary>How the match ended, or null while it is still running (§5.1).</summary>
    public MatchOutcome? Outcome { get; internal set; }

    /// <summary>
    /// The one source of card instance ids for the whole match, so a card
    /// created after setup never reuses a deck card's id.
    /// </summary>
    public CardInstanceIdGenerator CardIds { get; } = new();

    public WorldState(ulong seed, MatchMode matchMode)
    {
        Seed = seed;
        MatchMode = matchMode;
        Board = new BoardState();
        PlayerA = new PlayerZones(PlayerId.PlayerA);
        PlayerB = new PlayerZones(PlayerId.PlayerB);
    }

    public PlayerZones Zones(PlayerId player) => player switch
    {
        PlayerId.PlayerA => PlayerA,
        PlayerId.PlayerB => PlayerB,
        _ => throw new ArgumentOutOfRangeException(nameof(player)),
    };
}
