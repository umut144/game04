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
/// same resulting fractions). This is the settled version -- it resolves
/// the card/totem-fit question the first pass left open.
///
/// The field is 1200 wide, centered horizontally in the 1920-wide
/// viewport (360px margin each side). A unit column is 1200/6 = 200
/// wide; a totem cell is 2 columns = 400 wide (a totem cell is always
/// exactly 2 unit columns, forced by <c>concepts/board_field.JPG</c>'s
/// grid lines running continuously top to bottom).
///
/// Row heights are chosen so each shape fits its own axis with zero
/// distortion:
/// - A unit/card row is exactly 300 tall, so a 200x300 cell holds the
///   declared 60:90 (2:3) card ratio with zero padding and zero overlap
///   -- 200:300 reduces to exactly 2:3.
/// - A totem row is 240 tall, so the declared 160:128 (5:4) totem ratio
///   comes out 300 wide inside the 400-wide cell, leaving a clean,
///   deliberate 50px of centered margin either side.
/// Two totem rows (240 each) and two unit rows (300 each) sum to 1080,
/// 120px short of the 1200-tall viewport; rather than pad each unit row
/// internally (the card fits its row exactly, no internal margin), that
/// 120px is pushed to the *outside* of the field as a 60px top and 60px
/// bottom margin, symmetric with the 360px side margins.
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
    /// [0] top margin end / opponent totem row start
    /// [1] opponent totem row end / opponent unit row start
    /// [2] opponent unit row end / own unit row start (exact board mid-line)
    /// [3] own unit row end / own totem row start
    /// [4] own totem row end / bottom margin start
    /// 60px (0.05 of the 1200-tall reference) is reserved above [0] and
    /// below [4].
    /// </summary>
    public static readonly IReadOnlyList<float> RowFractions = new[]
    {
        0.05f,
        0.25f,
        0.5f,
        0.75f,
        0.95f,
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
