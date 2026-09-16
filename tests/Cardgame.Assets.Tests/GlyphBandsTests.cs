namespace Cardgame.Assets.Tests;

using Xunit;

public sealed class GlyphBandsTests
{
    [Theory]
    [InlineData(0, "---", 0)]
    [InlineData(1, "1--", 0)]
    [InlineData(2, "12-", 0)]
    [InlineData(3, "123", 0)]
    [InlineData(4, "--3", 1)]
    [InlineData(5, "-23", 1)]
    [InlineData(6, "123", 1)]
    [InlineData(7, "1--", 2)]
    [InlineData(8, "12-", 2)]
    [InlineData(9, "123", 2)]
    public void ValuesFillFromGlyph01ExceptFourToSixWhichFillFromGlyph03(int value, string pattern, int intensity)
    {
        string filled = string.Concat(Enumerable.Range(1, 3).Select(g => GlyphBands.IsFilled(value, g) ? g.ToString() : "-"));

        Assert.Equal(pattern, filled);
        if (value > 0)
        {
            Assert.Equal(intensity, GlyphBands.IntensityOf(value));
        }
    }

    [Fact]
    public void OutOfRangeInputIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphBands.IsFilled(10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphBands.IsFilled(3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphBands.IsFilled(3, 4));
    }
}
