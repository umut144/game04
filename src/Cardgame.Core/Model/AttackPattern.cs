namespace Cardgame.Core.Model;

/// <summary>What an attack affects once it has a target (§8.1, §8.2).</summary>
public enum AttackPattern
{
    /// <summary>The target only.</summary>
    Single,

    /// <summary>The target, hit twice in a row (Mage).</summary>
    DoubleHit,

    /// <summary>The target and the two slots beside it, full damage (Wizard).</summary>
    AreaThree,

    /// <summary>Full damage to the target, half — rounded down — to the two beside it (Warrior).</summary>
    Cleave,

    /// <summary>Equal damage to the target and the two beside it (Hammerer).</summary>
    Splash,

    /// <summary>Every other unit in both front rows, own included; no target (Sorcerer).</summary>
    BothFrontRows,
}

/// <summary>
/// How a card attacks: its Range (lateral offset it may aim at, §8.1) and
/// what the attack then affects. Reach and affected area are separate on
/// purpose. <see cref="Range"/> is null for a pattern that takes no target.
/// </summary>
public sealed record AttackProfile(int? Range, AttackPattern Pattern)
{
    public bool NeedsTarget => Pattern != AttackPattern.BothFrontRows;
}
