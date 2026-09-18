namespace Cardgame.Core.Events;

using Cardgame.Core.Board;
using Cardgame.Core.Model;

/// <summary>One unit hit by an attack, and whether it was destroyed.</summary>
public sealed record Hit(FieldRef Field, CardInstanceId Card, int Damage, bool Destroyed, int BountyPaid);

/// <summary>
/// One totem hit by an attack (§8.6): <paramref name="Amount"/> is health for
/// a Totem of Life, mana for a Totem of Mana and seconds for a Totem of Time.
/// <paramref name="Overdamage"/> marks the Time hit that took more than the
/// pool held and therefore ends the attacker's own turn.
/// </summary>
public sealed record TotemHit(TotemRef Totem, TotemType Type, int Amount, bool Overdamage);

public sealed record UnitAttackedEvent : IEvent
{
    public required PlayerId Player { get; init; }
    public required int AttackerSlot { get; init; }
    public required CardInstanceId Attacker { get; init; }
    public FieldRef? Target { get; init; }
    public TotemRef? TotemTarget { get; init; }
    public required IReadOnlyList<Hit> Hits { get; init; }
    public IReadOnlyList<TotemHit> TotemHits { get; init; } = Array.Empty<TotemHit>();

    /// <summary>Mana refunded to an Arcane attacker's controller for a kill (§7.6).</summary>
    public int ManaRefunded { get; init; }

    /// <summary>
    /// The turn this attack ended by overdamaging a Totem of Time (§8.6), and
    /// the turn that began instead. Null when the attacker keeps their turn.
    /// </summary>
    public TurnStartedEvent? TurnEnded { get; init; }

    /// <summary>Set when the attack destroyed a Totem of Life (§5.1).</summary>
    public MatchOutcome? Outcome { get; init; }
}
