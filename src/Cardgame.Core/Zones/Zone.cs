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

    public CardInstance? Find(CardInstanceId id) => _cards.FirstOrDefault(card => card.Id == id);

    public bool Remove(CardInstance card) => _cards.Remove(card);

    /// <summary>Removes and returns the top card (index 0), or null if empty.</summary>
    public CardInstance? TakeTop()
    {
        if (_cards.Count == 0)
        {
            return null;
        }

        var top = _cards[0];
        _cards.RemoveAt(0);
        return top;
    }

    public void ShuffleInPlace(Xoshiro256StarStar rng) => DeterministicShuffle.ShuffleInPlace(_cards, rng);
}
