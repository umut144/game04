namespace Cardgame.Core.Snapshot;

using Cardgame.Core;
using Cardgame.Core.Model;

/// <summary>Projects a full <see cref="WorldState"/> into what one player may see.</summary>
public static class ProjectionSystem
{
    public static PlayerView Project(WorldState world, PlayerId viewer)
    {
        PlayerId opponent = Opponent(viewer);
        var own = world.Zones(viewer);
        var other = world.Zones(opponent);

        return new PlayerView
        {
            Viewer = viewer,
            OwnHandCount = own.Hand.Cards.Count,
            OwnHandCards = own.Hand.Cards.Select(card => card.Id).ToArray(),
            OwnDeckCount = own.Deck.Cards.Count,
            OpponentHandCount = other.Hand.Cards.Count,
            OpponentDeckCount = other.Deck.Cards.Count,
            OwnDestroyed = own.Destroyed.Cards.Select(card => card.Id).ToArray(),
            OpponentDestroyed = other.Destroyed.Cards.Select(card => card.Id).ToArray(),
            Board = world.Board,
        };
    }

    private static PlayerId Opponent(PlayerId player) => player switch
    {
        PlayerId.PlayerA => PlayerId.PlayerB,
        PlayerId.PlayerB => PlayerId.PlayerA,
        _ => throw new ArgumentOutOfRangeException(nameof(player)),
    };
}
