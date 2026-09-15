namespace Cardgame.Core.Zones;

using Cardgame.Core.Model;
using Cardgame.Core.Rng;

/// <summary>One player's deck, hand or destroyed-cards list (§8.3).</summary>
public sealed class Zone
{
    private readonly List<CardInstance> _cards = new();

    public PlayerId Owner { get; }
    public ZoneVisibility Visibility { get; }
    public IReadOnlyList<CardInstance> Cards => _cards;

    public Zone(PlayerId owner, ZoneVisibility visibility)
    {
        Owner = owner;
        Visibility = visibility;
    }

    public void Add(CardInstance card) => _cards.Add(card);

    public void ShuffleInPlace(Xoshiro256StarStar rng) => DeterministicShuffle.ShuffleInPlace(_cards, rng);
}
