using System.Collections.Generic;

namespace Cardgame.Client.Presentation;

/// <summary>
/// G01 board grid geometry, expressed as fractions of the viewport rather
/// than raw pixels, so the layout carries over unchanged at any resolution
/// sharing the same 16:10 aspect. Derivation and numbers:
/// <c>docs/BOARD_DESIGN.md</c> (settled as <c>LAYOUT-01</c>).
///
/// In short, at the 1920x1200 reference: the field fills the full height and
/// is centred horizontally. Its proportions come from a 1200x1080 grid —
/// columns 200 wide, totem rows 240 tall, unit rows 300 tall — scaled by
/// 10/9 so that it touches the top and bottom edge: columns 222.2, totem
/// rows 266.7, unit rows 333.3, field 1333.3x1200, side margins 293.3.
/// A card fills its slot exactly (2:3); a totem is 3/4 of its cell's width
/// (5:4), centred. Non-integer pixel sizes are expected; the anchors take
/// care of them.
///
/// Presentation-only: nothing in Cardgame.Core reads this.
/// </summary>
public static class BoardLayoutSpec
{
    /// <summary>The resolution this layout was worked out against.</summary>
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
    /// Totem rows are 2/9 of the height, unit rows 5/18; no margin.
    /// </summary>
    public static readonly IReadOnlyList<float> RowFractions = new[]
    {
        0f,
        2f / 9f,
        1f / 2f,
        7f / 9f,
        1f,
    };

    /// <summary>
    /// Horizontal fractions (0..1) of the 7 column boundaries, left to right,
    /// shared by every row: columns 0..5 are the 6 unit slots; a totem cell
    /// spans boundaries (0,2), (2,4) or (4,6). Each column is 25/216 of the
    /// width, the side margins 11/72 each.
    /// </summary>
    public static readonly IReadOnlyList<float> ColumnFractions = new[]
    {
        33f / 216f,
        58f / 216f,
        83f / 216f,
        108f / 216f,
        133f / 216f,
        158f / 216f,
        183f / 216f,
    };

    /// <summary>
    /// Which pair of column-boundary indices (into <see cref="ColumnFractions"/>)
    /// each of the 3 totem cells spans, left to right — places A, B, C.
    /// </summary>
    public static readonly IReadOnlyList<(int Left, int Right)> TotemCellSpans = new[]
    {
        (0, 2),
        (2, 4),
        (4, 6),
    };

    /// <summary>
    /// The share of a totem cell's width left free on each side of the totem:
    /// the totem is 3/4 of the cell's width (5:4 at the row's height).
    /// </summary>
    public const float TotemSideInset = 1f / 8f;
}
