namespace Cardgame.Core.Design;

using Cardgame.Core.Model;

/// <summary>
/// One card as authored under design/cards/*.json - the game-designer-facing
/// shape. <see cref="CardDefinition"/> is what the simulation actually
/// consumes, built from this by <see cref="DesignCatalogLoader"/>.
/// </summary>
public sealed record CardDesign
{
    public required int SchemaVersion { get; init; }
    public required string Id { get; init; }
    public CardType? Type { get; init; }
    public required IReadOnlyList<string> AbilityNameKeys { get; init; }
    public required IReadOnlyList<CardTierDesign> Tiers { get; init; }
}
