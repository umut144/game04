namespace Cardgame.Core.Tests.Rng;

using Cardgame.Core.Rng;
using Xunit;

public sealed class Xoshiro256StarStarTests
{
    [Fact]
    public void SameSeedProducesTheSameSequence()
    {
        var first = new Xoshiro256StarStar(1234);
        var second = new Xoshiro256StarStar(1234);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(first.NextUInt64(), second.NextUInt64());
        }
    }

    [Fact]
    public void DifferentSeedsDiverge()
    {
        var first = new Xoshiro256StarStar(1);
        var second = new Xoshiro256StarStar(2);

        Assert.NotEqual(first.NextUInt64(), second.NextUInt64());
    }

    [Fact]
    public void NextIntStaysWithinBounds()
    {
        var rng = new Xoshiro256StarStar(99);

        for (int i = 0; i < 1000; i++)
        {
            int value = rng.NextInt(6);
            Assert.InRange(value, 0, 5);
        }
    }

    [Fact]
    public void ShuffleIsAPermutationOfTheOriginalItems()
    {
        var items = Enumerable.Range(0, 20).ToList();
        var rng = new Xoshiro256StarStar(7);

        DeterministicShuffle.ShuffleInPlace(items, rng);

        Assert.Equal(Enumerable.Range(0, 20).OrderBy(x => x), items.OrderBy(x => x));
    }
}
