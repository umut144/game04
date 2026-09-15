namespace Cardgame.Core.Board;

/// <summary>
/// The three totem places of one side, left to right: A stands behind
/// columns 1-2, B behind 3-4, C behind 5-6 (GAME_DESIGN.md §3). Named the
/// same way on both sides, like the unit slots, so A always opposes A;
/// which totem stands on a place is what differs.
/// </summary>
public enum TotemPosition
{
    A,
    B,
    C,
}
