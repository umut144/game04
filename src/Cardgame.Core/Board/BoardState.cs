namespace Cardgame.Core.Board;

using Cardgame.Core.Model;

/// <summary>Both sides' boards (§3), and the card instances standing on them.</summary>
public sealed class BoardState
{
    private readonly Dictionary<CardInstanceId, CardInstance> _units = new();

    public BoardSide PlayerA { get; } = new();
    public BoardSide PlayerB { get; } = new();

    /// <summary>Every card on either side, by id.</summary>
    public IReadOnlyDictionary<CardInstanceId, CardInstance> Units => _units;

    public BoardSide Side(PlayerId player) => player switch
    {
        PlayerId.PlayerA => PlayerA,
        PlayerId.PlayerB => PlayerB,
        _ => throw new ArgumentOutOfRangeException(nameof(player)),
    };

    internal void Place(CardInstance card) => _units[card.Id] = card;
}
