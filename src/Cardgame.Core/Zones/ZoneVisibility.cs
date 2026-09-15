namespace Cardgame.Core.Zones;

/// <summary>
/// Who may see a zone's card identities in a player's projection (CORE-02).
/// A zone's card *count* is always visible regardless (both players always
/// see how many cards are in a deck or hand); this only governs identities.
/// </summary>
public enum ZoneVisibility
{
    /// <summary>Both players see the card identities (e.g. the destroyed list, §8.3).</summary>
    Public,

    /// <summary>Only the owner sees the card identities (the hand).</summary>
    OwnerOnly,

    /// <summary>Nobody sees the card identities, not even the owner (the deck: order and contents are hidden from both sides).</summary>
    Hidden,
}
