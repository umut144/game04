namespace Cardgame.Core.Model;

/// <summary>
/// One of a card's three tiers (GAME_DESIGN.md §7.3): the four corner values
/// (§7.1), normalised from design data. Which currency the cost is paid in,
/// and everything about effects, arrives in later gates.
/// </summary>
public sealed record CardTier
{
    public required int Cost { get; init; }
    public required int Bounty { get; init; }
    public required int Attack { get; init; }
    public required int Health { get; init; }

    /// <summary>Abilities only this tier has, on top of the card's own.</summary>
    public IReadOnlyList<string> AbilityNameKeys { get; init; } = Array.Empty<string>();
}
