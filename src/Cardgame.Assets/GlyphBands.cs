namespace Cardgame.Assets;

/// <summary>
/// Which corner-wedge bands a value fills (GAME_DESIGN.md §7.2). A wedge has
/// four bands, <c>glyph01</c> (the largest, outermost) to <c>glyph04</c> (the
/// small one in the corner), and three colour intensities, so a value runs
/// from 1 to 12. A value v has intensity (v−1) / 4 and (v−1) % 4 + 1 filled
/// bands. The fill runs from glyph01 towards glyph04 for 1–4 and 9–12, and
/// from glyph04 towards glyph01 for 5–8, so neighbouring intensities read
/// apart even where their colours are close. 0 fills nothing.
/// </summary>
public static class GlyphBands
{
    /// <summary>Bands per wedge, one glyph each (VALUE-12).</summary>
    public const int BandCount = 4;

    public const int MaximumValue = BandCount * 3;

    /// <summary>Intensity 0, 1 or 2; only meaningful for 1–12.</summary>
    public static int IntensityOf(int value) => (value - 1) / BandCount;

    public static bool IsFilled(int value, int glyphNumber)
    {
        if (glyphNumber is < 1 || glyphNumber > BandCount)
        {
            throw new ArgumentOutOfRangeException(nameof(glyphNumber), glyphNumber, $"glyphs are numbered 1-{BandCount}");
        }

        if (value <= 0)
        {
            return false;
        }

        if (value > MaximumValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"values run from 0 to {MaximumValue}");
        }

        int filled = (value - 1) % BandCount + 1;
        return IntensityOf(value) == 1 ? glyphNumber > BandCount - filled : glyphNumber <= filled;
    }
}
