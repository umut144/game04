namespace Cardgame.Core.Rng;

/// <summary>A seeded, in-place Fisher-Yates shuffle.</summary>
public static class DeterministicShuffle
{
    public static void ShuffleInPlace<T>(IList<T> items, Xoshiro256StarStar rng)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
