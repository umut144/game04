namespace Cardgame.Core.Model;

/// <summary>
/// What an attack's shape does once it is aimed at the totem row (§8.6,
/// G06-04). A unit that keeps its area on the board does not automatically
/// keep it against totems: the Warrior throws his axe straight ahead, the
/// Hammerer's hammer keeps flying the way it does between units.
/// </summary>
public enum TotemPattern
{
    /// <summary>Only the totem that was aimed at, whatever the unit pattern is.</summary>
    Single,

    /// <summary>The unit pattern, applied across the totem row (Hammerer).</summary>
    Keep,
}
