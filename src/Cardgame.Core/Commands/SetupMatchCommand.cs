namespace Cardgame.Core.Commands;

using Cardgame.Core.Model;

/// <summary>
/// Sets a match up: both decks built from the given definitions and
/// shuffled, both totem layouts rolled — seeded, per the chosen mirror mode
/// (CORE-03). No starting hand is dealt (CORE-10).
/// </summary>
public sealed record SetupMatchCommand : ICommand
{
    public required ulong Seed { get; init; }
    public required MatchMode MatchMode { get; init; }
    public required IReadOnlyList<string> PlayerADeckDefinitionIds { get; init; }
    public required IReadOnlyList<string> PlayerBDeckDefinitionIds { get; init; }
}
