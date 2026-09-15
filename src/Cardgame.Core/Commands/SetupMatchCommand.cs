namespace Cardgame.Core.Commands;

using Cardgame.Core.Model;

/// <summary>
/// Sets a match up: build both decks from the given definitions and shuffle
/// them (seeded, per the chosen mirror mode - CORE-03) into the starting
/// WorldState. G00's match setup stops there - no starting hand is dealt yet
/// (deliberately: see docs/TASKS.md on why that is G04's job).
/// </summary>
public sealed record SetupMatchCommand : ICommand
{
    public required ulong Seed { get; init; }
    public required MirrorMode MirrorMode { get; init; }
    public required IReadOnlyList<string> PlayerADeckDefinitionIds { get; init; }
    public required IReadOnlyList<string> PlayerBDeckDefinitionIds { get; init; }
}
