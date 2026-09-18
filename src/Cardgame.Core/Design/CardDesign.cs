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

    /// <summary>The PolyTools character drawn on the card (SYNC-02); optional.</summary>
    public string? AssetKey { get; init; }
    public required IReadOnlyList<string> AbilityNameKeys { get; init; }
    public required IReadOnlyList<CardTierDesign> Tiers { get; init; }

    /// <summary>Omitted or null for a card that does not attack.</summary>
    public AttackDesign? Attack { get; init; }
}

public sealed record AttackDesign
{
    public int? Range { get; init; }
    public required AttackPattern Pattern { get; init; }

    /// <summary>What the pattern does against totems (§8.6); single by default.</summary>
    public TotemPattern TotemPattern { get; init; } = TotemPattern.Single;
}
