namespace Cardgame.Core.Board;

using Cardgame.Core.Model;

/// <summary>Which totem stands on which of a side's three places.</summary>
public readonly record struct TotemPlacement(TotemPosition Position, TotemType Type);
