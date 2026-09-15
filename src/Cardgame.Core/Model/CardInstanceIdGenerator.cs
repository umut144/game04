namespace Cardgame.Core.Model;

/// <summary>
/// Hands out stable instance ids in a fixed, reproducible order. One per
/// match (<see cref="WorldState.CardIds"/>); ids are reproducible because
/// every call happens in a deterministic order — deck build first, then
/// command by command.
/// </summary>
public sealed class CardInstanceIdGenerator
{
    private long _next;

    public CardInstanceId Next() => new(_next++);
}
