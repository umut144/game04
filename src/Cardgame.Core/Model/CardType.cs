namespace Cardgame.Core.Model;

/// <summary>
/// The nine types from the fiction (GAME_DESIGN.md §7.6). Rules attach to a
/// type one at a time, as a card actually needs them; G00 only carries which
/// type a card has, if any, with no rule attached to it yet.
/// </summary>
public enum CardType
{
    Arcane,
    Human,
    Goblin,
    Ghost,
    Wild,
    Kobold,
    Nature,
    Puppet,
    Bird,
}
