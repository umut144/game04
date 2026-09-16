namespace Cardgame.Core.Board;

using Cardgame.Core.Model;

/// <summary>One unit slot on one side of the board.</summary>
public readonly record struct FieldRef(PlayerId Side, int Slot)
{
    public override string ToString() => $"{Side}:{Slot}";
}
