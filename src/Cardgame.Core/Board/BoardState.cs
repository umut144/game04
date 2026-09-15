namespace Cardgame.Core.Board;

using Cardgame.Core.Model;

/// <summary>Both sides' boards (§3).</summary>
public sealed class BoardState
{
    public BoardSide PlayerA { get; } = new();
    public BoardSide PlayerB { get; } = new();

    public BoardSide Side(PlayerId player) => player switch
    {
        PlayerId.PlayerA => PlayerA,
        PlayerId.PlayerB => PlayerB,
        _ => throw new ArgumentOutOfRangeException(nameof(player)),
    };
}
