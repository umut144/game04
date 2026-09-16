namespace Cardgame.Core.Events;

using Cardgame.Core.Board;
using Cardgame.Core.Model;

/// <summary>One unit hit by an attack, and whether it was destroyed.</summary>
public sealed record Hit(FieldRef Field, CardInstanceId Card, int Damage, bool Destroyed, int BountyPaid);

public sealed record UnitAttackedEvent : IEvent
{
    public required PlayerId Player { get; init; }
    public required int AttackerSlot { get; init; }
    public required CardInstanceId Attacker { get; init; }
    public FieldRef? Target { get; init; }
    public required IReadOnlyList<Hit> Hits { get; init; }

    /// <summary>Mana refunded to an Arcane attacker's controller for a kill (§7.6).</summary>
    public int ManaRefunded { get; init; }
}
