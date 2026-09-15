namespace Cardgame.Core.Design;

/// <summary>One tier as authored in a card's design file (§7.3).</summary>
public sealed record CardTierDesign
{
    public required int Cost { get; init; }
    public required int Bounty { get; init; }
    public required int Attack { get; init; }
    public required int Health { get; init; }
}
