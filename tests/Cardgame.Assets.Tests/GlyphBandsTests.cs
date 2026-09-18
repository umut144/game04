namespace Cardgame.Assets.Tests;

using Xunit;

public sealed class GlyphBandsTests
{
    [Theory]
    [InlineData(0, "----", 0)]
    [InlineData(1, "---4", 0)]
    [InlineData(2, "--34", 0)]
    [InlineData(3, "-234", 0)]
    [InlineData(4, "1234", 0)]
    [InlineData(5, "1---", 1)]
    [InlineData(6, "12--", 1)]
    [InlineData(7, "123-", 1)]
    [InlineData(8, "1234", 1)]
    [InlineData(9, "---4", 2)]
    [InlineData(10, "--34", 2)]
    [InlineData(11, "-234", 2)]
    [InlineData(12, "1234", 2)]
    public void ValuesFillFromGlyph04ExceptFiveToEightWhichFillFromGlyph01(int value, string pattern, int intensity)
    {
        string filled = string.Concat(Enumerable
            .Range(1, GlyphBands.BandCount)
            .Select(g => GlyphBands.IsFilled(value, g) ? g.ToString() : "-"));

        Assert.Equal(pattern, filled);
        if (value > 0)
        {
            Assert.Equal(intensity, GlyphBands.IntensityOf(value));
        }
    }

    [Fact]
    public void OutOfRangeInputIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphBands.IsFilled(13, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphBands.IsFilled(3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphBands.IsFilled(3, 5));
    }
}
