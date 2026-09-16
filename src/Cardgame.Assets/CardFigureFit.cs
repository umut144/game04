namespace Cardgame.Assets;

/// <summary>
/// Places a character on a card: centred on the card, uniformly scaled as
/// large as it can be while its bounding box stays inside the card and clear
/// of the four <c>*_glyph01</c> corner wedges (G02-05). The box may be either
/// narrow and tall (between the left and right wedges) or wide and short
/// (between the top and bottom wedges); whichever allows the larger figure
/// wins. Every figure gets its own factor this way, whatever its size in
/// PolyTools.
/// </summary>
public static class CardFigureFit
{
    public const string CornerGlyphSuffix = "_glyph01";

    /// <summary>The free inner area of a card: half-extents around its centre.</summary>
    public readonly record struct CardWindow(float CenterX, float CenterY, float HalfWidth, float HalfHeight, float InnerHalfWidth, float InnerHalfHeight);

    public static CardWindow WindowOf(AssetGeometry card)
    {
        float cx = card.CenterX;
        float cy = card.CenterY;
        float innerX = float.PositiveInfinity;
        float innerY = float.PositiveInfinity;
        var glyphs = card.Parts
            .Where(part => part.Kind == AssetPartKind.Fill && part.ComponentName.EndsWith(CornerGlyphSuffix, StringComparison.Ordinal))
            .ToArray();
        if (glyphs.Length == 0)
        {
            throw new ManifestException($"card '{card.AssetKey}' has no *{CornerGlyphSuffix} corner wedges");
        }

        foreach (var glyph in glyphs)
        {
            var xs = glyph.Vertices.Where((_, i) => i % 2 == 0).ToArray();
            var ys = glyph.Vertices.Where((_, i) => i % 2 == 1).ToArray();
            innerX = Math.Min(innerX, xs.Average() < cx ? cx - xs.Max() : xs.Min() - cx);
            innerY = Math.Min(innerY, ys.Average() < cy ? cy - ys.Max() : ys.Min() - cy);
        }

        return new CardWindow(cx, cy, card.Width / 2f, card.Height / 2f, innerX, innerY);
    }

    /// <summary>
    /// The figure (built at scale 1) scaled and moved into the card's window,
    /// in the card's coordinates.
    /// </summary>
    public static AssetGeometry Fit(AssetGeometry card, AssetGeometry figure, string layer)
    {
        var window = WindowOf(card);
        float scale = ScaleFor(window, figure.Width, figure.Height);
        return figure.Placed(
            scale,
            window.CenterX - scale * figure.CenterX,
            window.CenterY - scale * figure.CenterY,
            layer);
    }

    public static float ScaleFor(CardWindow window, float width, float height)
    {
        if (!(width > 0) || !(height > 0))
        {
            throw new ManifestException("a figure needs a positive width and height");
        }

        float tall = Math.Min(2 * window.InnerHalfWidth / width, 2 * window.HalfHeight / height);
        float wide = Math.Min(2 * window.HalfWidth / width, 2 * window.InnerHalfHeight / height);
        return Math.Max(tall, wide);
    }
}
