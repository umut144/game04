using Cardgame.Assets;
using Cardgame.Core.Model;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// The value language on the card (GAME_DESIGN.md §7.1, §7.2): each corner
/// wedge has four glyphs, and <see cref="GlyphBands"/> says which of them a
/// value fills; they take the value's intensity of its colour family. Colours are
/// chosen here for G02 and move to design/ once they need tuning.
/// </summary>
public static class CardColours
{
    private static readonly Color[] Mana = { new("9CCBF0"), new("3F86D6"), new("1C3F8C") };
    private static readonly Color[] Coins = { new("B87333"), new("C0C7CE"), new("D4AF37") };
    private static readonly Color[] Attack = { new("F28B82"), new("D93025"), new("8B1A1A") };
    private static readonly Color[] Health = { new("9BD99B"), new("3DA548"), new("1E6B2B") };

    public static Color Of(AssetPart part, CardTier? values, BoardAssets assets)
    {
        if (part.Layer == BoardAssets.FigureLayer)
        {
            return part.Kind == AssetPartKind.Fill ? assets.FigureFill : assets.FigureStroke;
        }

        if (part.Kind == AssetPartKind.Stroke || values is null || !TryParseGlyph(part.ComponentName, out string corner, out int glyph))
        {
            return part.Kind == AssetPartKind.Fill ? assets.CardFill : assets.CardStroke;
        }

        var (value, family) = corner switch
        {
            "mana" => (values.Cost, Mana),
            "bounty" => (values.Bounty, Coins),
            "attack" => (values.Attack, Attack),
            "health" => (values.Health, Health),
            _ => (0, Mana),
        };

        if (!GlyphBands.IsFilled(System.Math.Clamp(value, 0, GlyphBands.MaximumValue), glyph))
        {
            return assets.CardFill;
        }

        return family[GlyphBands.IntensityOf(System.Math.Clamp(value, 1, GlyphBands.MaximumValue))];
    }

    private static bool TryParseGlyph(string name, out string corner, out int glyph)
    {
        corner = string.Empty;
        glyph = 0;
        int at = name.IndexOf("_glyph0", System.StringComparison.Ordinal);
        if (at <= 0 || at + 8 != name.Length || !int.TryParse(name.Substring(at + 7), out int number)
            || number < 1 || number > GlyphBands.BandCount)
        {
            return false;
        }

        corner = name[..at];
        glyph = number;
        return true;
    }
}
