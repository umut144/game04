namespace Cardgame.Core.Snapshot;

using Cardgame.Core.Board;
using Cardgame.Core.Model;

/// <summary>
/// What one player is allowed to see (CORE-02), built by
/// <see cref="ProjectionSystem"/>. There is deliberately no property here
/// that exposes the opponent's hand contents or either side's deck order -
/// CORE-02's visibility rule expressed as a type, not as a check someone has
/// to remember to apply.
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
    public required BoardState Board { get; init; }
}
