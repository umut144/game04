namespace Cardgame.Assets;

/// <summary>
/// Which corner-wedge bands a value fills (GAME_DESIGN.md §7.2). A value v
/// has intensity (v−1) / 3 and (v−1) % 3 + 1 filled bands. The fill runs from
/// glyph01 towards glyph03 for 1–3 and 7–9, and from glyph03 towards glyph01
/// for 4–6, so neighbouring intensities read apart even where their colours
/// are close. 0 fills nothing.
/// </summary>
public static class GlyphBands
{
    public const int MaximumValue = 9;

    /// <summary>Intensity 0, 1 or 2; only meaningful for 1–9.</summary>
    public static int IntensityOf(int value) => (value - 1) / 3;

    public static bool IsFilled(int value, int glyphNumber)
    {
        if (glyphNumber is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(glyphNumber), glyphNumber, "glyphs are numbered 1-3");
        }

        if (value <= 0)
        {
            return false;
        }

        if (value > MaximumValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"values run from 0 to {MaximumValue}");
        }

        int filled = (value - 1) % 3 + 1;
        return IntensityOf(value) == 1 ? glyphNumber > 3 - filled : glyphNumber <= filled;
    }
}
