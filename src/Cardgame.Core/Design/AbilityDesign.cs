namespace Cardgame.Core.Design;

/// <summary>
/// One ability as authored under design/abilities/*.json. G00 only needs
/// enough here to prove that a card's ability_name_keys resolve to
/// something real; the trigger/effect/value-symbol structure is G07.
/// </summary>
public sealed record AbilityDesign
{
    public required int SchemaVersion { get; init; }
    public required string NameKey { get; init; }
}
