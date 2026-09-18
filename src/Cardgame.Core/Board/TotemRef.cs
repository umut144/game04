namespace Cardgame.Core.Board;

using Cardgame.Core.Model;

/// <summary>One totem place on one side of the board (§4).</summary>
public readonly record struct TotemRef(PlayerId Side, TotemPosition Position)
{
    public TotemColumnPair Columns => TotemColumnPair.StandardPairs[(int)Position];

    public override string ToString() => $"{Side}:{Position}";
}
