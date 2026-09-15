namespace Cardgame.Core.Rng;

/// <summary>
/// A small, fully specified, portable RNG stream - deliberately not
/// System.Random, whose output is not a stable, documented sequence across
/// .NET versions and is therefore unfit for "both sides provably get the
/// same order" (CORE-03). One instance is one stream: every purpose (deck
/// order today, totem layout from G01 on) gets its own instance, seeded via
/// <see cref="SeedDerivation.DeriveSubSeed"/>.
/// </summary>
public sealed class Xoshiro256StarStar
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public Xoshiro256StarStar(ulong seed)
    {
        // Seed the four-word state from one 64-bit seed via SplitMix64 - the
        // standard way to initialise a xoshiro/xoroshiro generator.
        _s0 = SplitMix64Step(ref seed);
        _s1 = SplitMix64Step(ref seed);
        _s2 = SplitMix64Step(ref seed);
        _s3 = SplitMix64Step(ref seed);
    }

    public ulong NextUInt64()
    {
        ulong result = RotateLeft(_s1 * 5, 7) * 9;

        ulong t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = RotateLeft(_s3, 45);

        return result;
    }

    /// <summary>A uniform value in [0, exclusiveUpperBound), via Lemire's
    /// method - unbiased, unlike a plain modulo.</summary>
    public int NextInt(int exclusiveUpperBound)
    {
        if (exclusiveUpperBound <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
        }

        ulong range = (ulong)exclusiveUpperBound;
        ulong threshold = unchecked((ulong)(-(long)range)) % range;

        while (true)
        {
            ulong value = NextUInt64();
            System.UInt128 product = (System.UInt128)value * range;
            ulong low = (ulong)product;
            if (low >= threshold)
            {
                return (int)(ulong)(product >> 64);
            }
        }
    }

    private static ulong SplitMix64Step(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static ulong RotateLeft(ulong value, int offset) => (value << offset) | (value >> (64 - offset));
}
