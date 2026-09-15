namespace Cardgame.Core.Model;

public static class PlayerIds
{
    public static PlayerId Opponent(PlayerId player) => player switch
    {
        PlayerId.PlayerA => PlayerId.PlayerB,
        PlayerId.PlayerB => PlayerId.PlayerA,
        _ => throw new ArgumentOutOfRangeException(nameof(player)),
    };
}
