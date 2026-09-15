using System.Collections.Generic;

namespace Cardgame.App.Presentation;

/// <summary>
/// G01 board grid geometry, expressed as fractions of the design viewport
/// (2560x1600, 16:10) rather than raw pixels, so the layout carries over
/// unchanged if the App ever renders at a different resolution or aspect
/// ratio: every consumer multiplies these fractions by the *current*
/// viewport size instead of reading a baked-in pixel constant.
///
/// The numbers below are the resolution of a concrete constraint, not an
/// arbitrary choice: <c>concepts/board_field.JPG</c> lays the board out as
/// 6 unit-slot columns per side, with each totem cell spanning exactly 2 of
/// those columns. That structural rule forces
/// <c>TotemCellWidth == 2 * UnitColumnWidth</c> whenever the grid lines are
/// meant to run continuously from the totem row into the unit rows below
/// it. Given that constraint plus the two footprints the developer asked
/// the grid to honour -- Card 60x90cm (2:3) and Totem 160x128cm (5:4) --
/// there is exactly one column width that lets both shapes sit in their
/// cells with zero distortion and zero padding: half the totem width
/// (80cm-equivalent), which uniformly scales a 60x90 card up to an
/// effective 80x120 without touching its 2:3 ratio. See the G01 spec
/// thread in docs/TASKS.md for the alternatives that were rejected (a
/// literal 60cm column forces the totem down to 120x96; decoupling the two
/// scales entirely makes the totem row ~385px wider than the unit columns
/// beneath it) and for how the remaining degree of freedom -- how much
/// vertical margin to reserve for hand/HUD chrome top and bottom -- was
/// picked (200px total, split evenly).
///
/// This is presentation-only geometry. It has no bearing on and is not
/// read by anything in Cardgame.Core; BoardSide's slot/column-pair model
/// is unaffected.
/// </summary>
public static class BoardLayoutSpec
{
    /// <summary>The resolution this layout was designed against.</summary>
    public const int DesignViewportWidth = 2560;
    public const int DesignViewportHeight = 1600;

    public const int UnitColumnCount = 6;
    public const int TotemCellCount = 3;

    /// <summary>
    /// Vertical fractions (0..1) of the 5 row boundaries, top to bottom:
    /// [0] top margin / opponent totem row start
    /// [1] opponent totem row end / opponent unit row start
    /// [2] opponent unit row end / own unit row start (exact board mid-line)
    /// [3] own unit row end / own totem row start
    /// [4] own totem row end / bottom margin start
    /// </summary>
    public static readonly IReadOnlyList<float> RowFractions = new[]
    {
        0.0625f,
        0.28830645f,
        0.5f,
        0.71169355f,
        0.9375f,
    };

    /// <summary>
    /// Horizontal fractions (0..1) of the 7 column boundaries, left to
    /// right, shared by every row: columns 0..5 are the 6 unit slots;
    /// a totem cell spans boundaries (0,2), (2,4) or (4,6).
    /// </summary>
    public static readonly IReadOnlyList<float> ColumnFractions = new[]
    {
        0.23538306f,
        0.32358871f,
        0.41179435f,
        0.5f,
        0.58820565f,
        0.67641129f,
        0.76461694f,
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
