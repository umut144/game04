namespace Cardgame.Core.Board;

/// <summary>
/// One of a side's three totem column pairs (§3, §4). G00 models only the
/// geometry - three anonymous pairs per side. Which totem (Life/Mana/Time)
/// sits in which pair, and its randomisation, is G01's job, not G00's
/// (CORE-08).
/// </summary>
public readonly record struct TotemColumnPair(int FirstColumn, int SecondColumn)
{
    public static IReadOnlyList<TotemColumnPair> StandardPairs { get; } = new[]
    {
        new TotemColumnPair(1, 2),
        new TotemColumnPair(3, 4),
        new TotemColumnPair(5, 6),
    };
}
