namespace Cardgame.Core.Tests.Rng;

using Cardgame.Core.Rng;
using Xunit;

public sealed class SeedDerivationTests
{
    [Fact]
    public void SameRootSeedAndPurposeProduceTheSameSubSeed()
    {
        ulong first = SeedDerivation.DeriveSubSeed(42, "deck-order");
        ulong second = SeedDerivation.DeriveSubSeed(42, "deck-order");

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentPurposesProduceDifferentSubSeeds()
    {
        ulong deckOrder = SeedDerivation.DeriveSubSeed(42, "deck-order");
        ulong totemLayout = SeedDerivation.DeriveSubSeed(42, "totem-layout");

        Assert.NotEqual(deckOrder, totemLayout);
    }

    [Fact]
    public void DifferentRootSeedsProduceDifferentSubSeeds()
    {
        ulong first = SeedDerivation.DeriveSubSeed(1, "deck-order");
        ulong second = SeedDerivation.DeriveSubSeed(2, "deck-order");

        Assert.NotEqual(first, second);
    }
}
