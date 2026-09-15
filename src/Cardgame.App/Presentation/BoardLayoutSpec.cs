using System.Collections.Generic;

namespace Cardgame.App.Presentation;

/// <summary>
/// G01 board grid geometry, expressed as fractions of the design viewport
/// rather than raw pixels, so the layout carries over unchanged at any
/// resolution sharing the same 16:10 aspect: every consumer multiplies
/// these fractions by the *current* viewport size instead of reading a
/// baked-in pixel constant.
///
/// The numbers below are the developer's own board-grid derivation
/// (recorded in full in <c>docs/TASKS.md</c>'s <c>LAYOUT-01</c> entry),
/// worked out against a 1920x1200 reference (chosen only because the
/// arithmetic is easier at that size; it is exactly 3/4 of the 2560x1600
/// design viewport used earlier in the G01 spec round, same 16:10 ratio,
/// same resulting fractions):
///
/// The board is a 1200x1200 square centered horizontally in the
/// 1920-wide viewport (360px margin each side) but using the *full*
/// viewport height — no vertical margin is reserved in this pass, so
/// hand/HUD chrome would need to live in the generous side margins
/// instead of a top/bottom bar. The square splits into two 1200x600
/// halves (opponent / own); each half's 600 splits 240 (totem row) :
/// 360 (unit row), a 2:5 / 3:5 split chosen directly by the developer,
/// not derived from the footprints. A unit column is 1200/6 = 200 wide;
/// a totem cell is 2 columns = 400 wide.
///
/// Both target footprints check out via their own aspect ratio at these
/// row heights: a totem at height 240 and the declared 160:128 (5:4)
/// ratio comes out 300 wide, leaving 50px of centered margin either side
/// of its 400-wide cell. A card at height 360 and the declared 60:90
/// (2:3) ratio comes out 240 wide -- 40 *wider* than its 200-wide column,
/// the opposite situation from the totem. Whether that's meant to read as
/// cards overlapping their neighbours by ~20px a side, or whether cards
/// should instead be held to the 200 column width (200x300, with 30px of
/// vertical margin top/bottom to match how the totem gets margin) is an
/// open question in `LAYOUT-01` -- this spec only encodes the grid's slot
/// *boundaries*, not how card art is meant to sit inside a slot once that
/// question is settled.
///
/// This is presentation-only geometry. It has no bearing on and is not
/// read by anything in Cardgame.Core; BoardSide's slot/column-pair model
/// is unaffected.
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
    /// [0] top edge of the board / opponent totem row start
    /// [1] opponent totem row end / opponent unit row start
    /// [2] opponent unit row end / own unit row start (exact board mid-line)
    /// [3] own unit row end / own totem row start
    /// [4] own totem row end / bottom edge of the board
    /// No margin is reserved above [0] or below [4] in this pass -- the
    /// board currently uses the full viewport height.
    /// </summary>
    public static readonly IReadOnlyList<float> RowFractions = new[]
    {
        0.0f,
        0.2f,
        0.5f,
        0.8f,
        1.0f,
    };

    /// <summary>
    /// Horizontal fractions (0..1) of the 7 column boundaries, left to
    /// right, shared by every row: columns 0..5 are the 6 unit slots;
    /// a totem cell spans boundaries (0,2), (2,4) or (4,6). 360px of
    /// margin (0.1875 of the 1920-wide reference) is reserved on each
    /// side for hand/HUD chrome.
    /// </summary>
    public static readonly IReadOnlyList<float> ColumnFractions = new[]
    {
        0.1875000f,
        0.2916667f,
        0.3958333f,
        0.5000000f,
        0.6041667f,
        0.7083333f,
        0.8125000f,
    };

    /// <summary>
    /// Which pair of column-boundary indices (into <see cref="ColumnFractions"/>)
    /// each of the 3 totem cells spans, left to right. Matches
    /// <c>Cardgame.Core.Board.TotemColumnPair.StandardPairs</c> in spirit
    /// (columns 1&amp;2, 3&amp;4, 5&amp;6 in 1-based unit-slot numbering).
    /// </summary>
    public static readonly IReadOnlyList<(int Left, int Right)> TotemCellSpans = new[]
    {
        (0, 2),
        (2, 4),
        (4, 6),
    };
}
