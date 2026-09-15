using System.Collections.Generic;

namespace Cardgame.Client.Presentation;

/// <summary>
/// One board grid, expressed as fractions of the viewport rather than raw
/// pixels, so it carries over unchanged at any resolution sharing the same
/// 16:10 aspect. Derivation and numbers: <c>docs/BOARD_DESIGN.md</c>.
///
/// Two layouts are on the table (<c>LAYOUT-02</c>) and <see cref="BoardScreen"/>
/// switches between them with keys 1 and 2:
/// - <see cref="Classic"/>: 60×90 cards. A 1200×1080 grid (columns 200,
///   totem rows 240, card rows 300) scaled by 10/9 to the full height.
/// - <see cref="WideCards"/>: 70×100 cards at the same px-per-cm, totems
///   unchanged. A 1400×1146.7 grid (columns 233.3, totem rows 240, card rows
///   333.3) scaled by 45/43 to the full height.
/// In both, a card fills its slot exactly and a totem is 300×240 before
/// scaling, centred in its two-column cell. Non-integer pixel sizes are
/// expected; the anchors take care of them.
///
/// Presentation-only: nothing in Cardgame.Core reads this.
/// </summary>
public sealed class BoardLayoutSpec
{
    /// <summary>The resolution the layouts were worked out against.</summary>
    public const int DesignViewportWidth = 1920;
    public const int DesignViewportHeight = 1200;

    public const int UnitColumnCount = 6;
    public const int TotemCellCount = 3;

    /// <summary>
    /// Which pair of column-boundary indices each of the 3 totem cells spans,
    /// left to right — places A, B, C. The same in every layout.
    /// </summary>
    public static readonly IReadOnlyList<(int Left, int Right)> TotemCellSpans = new[]
    {
        (0, 2),
        (2, 4),
        (4, 6),
    };

    public static readonly BoardLayoutSpec Classic = new(
        "1: cards 60×90, factor 10/9",
        new[] { 0f, 2f / 9f, 1f / 2f, 7f / 9f, 1f },
        new[] { 33f / 216f, 58f / 216f, 83f / 216f, 108f / 216f, 133f / 216f, 158f / 216f, 183f / 216f },
        1f / 8f);

    public static readonly BoardLayoutSpec WideCards = new(
        "2: cards 70×100, factor 45/43",
        new[] { 0f, 9f / 43f, 1f / 2f, 34f / 43f, 1f },
        new[] { 163f / 1376f, 338f / 1376f, 513f / 1376f, 688f / 1376f, 863f / 1376f, 1038f / 1376f, 1213f / 1376f },
        5f / 28f);

    private BoardLayoutSpec(
        string name,
        IReadOnlyList<float> rowFractions,
        IReadOnlyList<float> columnFractions,
        float totemSideInset)
    {
        Name = name;
        RowFractions = rowFractions;
        ColumnFractions = columnFractions;
        TotemSideInset = totemSideInset;
    }

    public string Name { get; }

    /// <summary>
    /// Vertical fractions (0..1) of the 5 row boundaries, top to bottom:
    /// [0] top edge / opponent totem row start
    /// [1] opponent totem row end / opponent unit row start
    /// [2] opponent unit row end / own unit row start (exact mid-line)
    /// [3] own unit row end / own totem row start
    /// [4] own totem row end / bottom edge
    /// </summary>
    public IReadOnlyList<float> RowFractions { get; }

    /// <summary>
    /// Horizontal fractions (0..1) of the 7 column boundaries, left to right,
    /// shared by every row: columns 0..5 are the 6 unit slots.
    /// </summary>
    public IReadOnlyList<float> ColumnFractions { get; }

    /// <summary>The share of a totem cell's width left free on each side of the totem.</summary>
    public float TotemSideInset { get; }
}
