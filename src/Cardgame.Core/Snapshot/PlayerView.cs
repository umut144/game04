namespace Cardgame.Core.Snapshot;

using Cardgame.Core.Model;

/// <summary>
/// What one player is allowed to see (CORE-02), built by
/// <see cref="ProjectionSystem"/> as a copy: a view taken once does not
/// change when the world does. There is deliberately no property that
/// exposes the opponent's hand contents or either side's deck order. The
/// board is told from the viewer's side — own and opponent — so a client
/// never has to work out which side is its own.
/// </summary>
public sealed record PlayerView
{
    public required PlayerId Viewer { get; init; }
    public required int OwnHandCount { get; init; }
    public required IReadOnlyList<CardInstanceId> OwnHandCards { get; init; }
    public required int OwnDeckCount { get; init; }
    public required int OpponentHandCount { get; init; }
    public required int OpponentDeckCount { get; init; }
    public required IReadOnlyList<CardInstanceId> OwnDestroyed { get; init; }
    public required IReadOnlyList<CardInstanceId> OpponentDestroyed { get; init; }
    public required PlayerId ActivePlayer { get; init; }
    public required int Round { get; init; }
    public required int BaseSeconds { get; init; }
    public required int BonusSeconds { get; init; }
    public bool IsOwnTurn => ActivePlayer == Viewer;
    public required int OwnMana { get; init; }
    public required int OwnMaxMana { get; init; }
    public required int OpponentMana { get; init; }
    public required int OpponentMaxMana { get; init; }
    public required BoardSideView OwnBoard { get; init; }
    public required BoardSideView OpponentBoard { get; init; }

    /// <summary>
    /// Every card the viewer may identify: their own hand and both boards.
    /// Opponent hand and all decks are absent by construction.
    /// </summary>
    public required IReadOnlyDictionary<CardInstanceId, CardView> Cards { get; init; }
}
