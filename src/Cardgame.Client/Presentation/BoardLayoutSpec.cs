using System.Collections.Generic;

namespace Cardgame.Client.Presentation;

/// <summary>
/// The board grid, expressed as fractions of the viewport rather than raw
/// pixels, so it carries over unchanged at any resolution sharing the same
/// 16:10 aspect. Derivation and how it was chosen: <c>docs/BOARD_DESIGN.md</c>
/// (<c>LAYOUT-01</c>).
///
/// At the 1920×1200 reference: 7:10 cards 240×342.9, totem rows 257.1 tall,
/// the field 1440×1200 filling the full height, and each side margin exactly
/// one card wide — room for the hand on the left and the deck on the right.
/// Columns, cards and side margins are each 1/8 of the width. A card fills
/// its slot exactly; a totem stands bottom-centre in its two-column cell,
/// sized by its game04 scale (design/asset_presentation.json).
/// Non-integer pixel sizes are expected; the anchors take care of them.
///
/// Presentation-only: nothing in Cardgame.Core reads this.
/// </summary>
public static class BoardLayoutSpec
{
    /// <summary>The resolution the layout was worked out against.</summary>
    public const int DesignViewportWidth = 1920;
    public const int DesignViewportHeight = 1200;

    public const int UnitColumnCount = 6;
    public const int TotemCellCount = 3;

    /// <summary>
    /// Vertical fractions (0..1) of the 5 row boundaries, top to bottom:
    /// [0] top edge / opponent totem row start
    /// [1] opponent totem row end / opponent unit row start
    /// [2] opponent unit row end / own unit row start (exact mid-line)
    /// [3] own unit row end / own totem row start
    /// [4] own totem row end / bottom edge
    /// Totem rows are 3/14 of the height, card rows 2/7; no margin.
    /// </summary>
    public static readonly IReadOnlyList<float> RowFractions = new[]
    {
        0f,
        3f / 14f,
        1f / 2f,
        11f / 14f,
        1f,
    };

    /// <summary>
    /// Horizontal fractions (0..1) of the 7 column boundaries, left to right,
    /// shared by every row: columns 0..5 are the 6 unit slots. Every column
    /// and each side margin is 1/8 of the width.
    /// </summary>
    public static readonly IReadOnlyList<float> ColumnFractions = new[]
    {
        1f / 8f,
        2f / 8f,
        3f / 8f,
        4f / 8f,
        5f / 8f,
        6f / 8f,
        7f / 8f,
    };

    /// <summary>
    /// Which pair of column-boundary indices each of the 3 totem cells spans,
    /// left to right — places A, B, C.
    /// </summary>
    public static readonly IReadOnlyList<(int Left, int Right)> TotemCellSpans = new[]
    {
        (0, 2),
        (2, 4),
        (4, 6),
    };
}
