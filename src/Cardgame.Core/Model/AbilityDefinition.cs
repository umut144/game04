namespace Cardgame.Core.Model;

/// <summary>
/// An ability, resolved and known to exist. G00 only needs enough here to
/// prove that a card's ability name keys resolve to something real; the
/// trigger/effect/value-symbol structure itself is G07 (§9, §15 "Tick
/// checkpoints").
/// </summary>
public sealed record AbilityDefinition
{
    public required string NameKey { get; init; }
}
