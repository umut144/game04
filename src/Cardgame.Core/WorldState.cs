namespace Cardgame.Core;

using Cardgame.Core.Board;
using Cardgame.Core.Model;
using Cardgame.Core.Zones;

/// <summary>
/// The whole state of one match (§3, §6, §7): board geometry, zones and the
/// match seed. Everything that reads or writes this state beyond setup
/// (turn clock enforcement, card play, combat, ...) arrives gate by gate.
/// </summary>
public sealed class WorldState
{
    public ulong Seed { get; }
    public MirrorMode MirrorMode { get; }
    public BoardState Board { get; }
    public PlayerZones PlayerA { get; }
    public PlayerZones PlayerB { get; }

    public WorldState(ulong seed, MirrorMode mirrorMode)
    {
        Seed = seed;
        MirrorMode = mirrorMode;
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
