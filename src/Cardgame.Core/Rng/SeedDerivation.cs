namespace Cardgame.Core.Rng;

using System.Text;

/// <summary>
/// Derives independent sub-seeds from one root seed (CORE-03). Each named
/// purpose gets its own stream, so a new randomised system added later never
/// perturbs an existing one's sequence. Perfect Mirror deliberately reuses
/// one derived seed's purpose label on both sides; Shuffled Mirror
/// deliberately gives each side its own label.
/// </summary>
public static class SeedDerivation
{
    public static ulong DeriveSubSeed(ulong rootSeed, string purpose)
    {
        ulong purposeHash = Fnv1A64(purpose);
        return SplitMix64Step(rootSeed ^ purposeHash);
    }

    private static ulong Fnv1A64(string text)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        ulong hash = offsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes(text))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }

    private static ulong SplitMix64Step(ulong state)
    {
        ulong z = state + 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
