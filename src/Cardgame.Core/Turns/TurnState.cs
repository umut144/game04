namespace Cardgame.Core.Turns;

using Cardgame.Core.Model;

/// <summary>
/// Whose turn it is (§6). A turn is one player's; a round is both players
/// having had one, and starts with the starting player's turn.
/// </summary>
public sealed class TurnState
{
    public PlayerId StartingPlayer { get; internal set; }
    public PlayerId ActivePlayer { get; internal set; }

    /// <summary>1 from the first turn on; 0 before the match has started.</summary>
    public int Round { get; internal set; }

    /// <summary>
    /// The last round in which a Totem of Life took damage; 0 while none has.
    /// Eight quiet rounds end the match in a draw (§5.1, G06-05).
    /// </summary>
    public int LastLifeDamageRound { get; internal set; }
}
