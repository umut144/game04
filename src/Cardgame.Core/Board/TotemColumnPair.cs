namespace Cardgame.Core.Board;

/// <summary>
/// The two unit columns a totem place stands behind (§3, §4).
/// <see cref="StandardPairs"/> is indexed by <see cref="TotemPosition"/>.
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
