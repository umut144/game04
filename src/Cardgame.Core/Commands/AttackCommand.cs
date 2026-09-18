namespace Cardgame.Core.Commands;

using Cardgame.Core.Board;
using Cardgame.Core.Model;

/// <summary>
/// The unit on <see cref="AttackerSlot"/> of the player's side attacks. The
/// target is an opposing slot, or null for a pattern that takes none.
/// </summary>
public sealed record AttackCommand : ICommand
{
    public required PlayerId Player { get; init; }
    public required int AttackerSlot { get; init; }
    public FieldRef? Target { get; init; }

    /// <summary>An opposing totem place instead of a slot (§8.6, G06).</summary>
    public TotemRef? TotemTarget { get; init; }
}
