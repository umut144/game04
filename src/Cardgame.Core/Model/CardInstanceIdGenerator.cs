namespace Cardgame.Core.Model;

/// <summary>
/// Hands out stable instance ids in a fixed, reproducible order. G00 only
/// ever calls this during match setup, in the deck's already-deterministic
/// build order, so "reproducible" today falls out of that order rather than
/// needing its own seed.
/// </summary>
public sealed class CardInstanceIdGenerator
{
    private long _next;

    public CardInstanceId Next() => new(_next++);
}
