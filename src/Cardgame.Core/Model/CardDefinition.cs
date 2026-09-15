namespace Cardgame.Core.Model;

/// <summary>
/// A card as the simulation consumes it: three tiers, an optional type, and
/// the ability name keys it plays. Built from
/// <see cref="Design.CardDesign"/> by <see cref="Design.DesignCatalogLoader"/>
/// - this is the seam between the game-designer-facing JSON shape and what
/// Systems actually read.
/// </summary>
public sealed record CardDefinition
{
    public required string Id { get; init; }
    public CardType? Type { get; init; }
    public required IReadOnlyList<string> AbilityNameKeys { get; init; }
    public required IReadOnlyList<CardTier> Tiers { get; init; }
}
