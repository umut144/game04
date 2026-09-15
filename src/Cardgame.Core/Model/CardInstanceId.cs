namespace Cardgame.Core.Model;

/// <summary>
/// A card instance's stable id (cardgame-ref §17 "Card Instances and Entity
/// IDs"): needed because two copies of the same definition can carry
/// different runtime state once later gates add any.
/// </summary>
public readonly record struct CardInstanceId(long Value)
{
    public override string ToString() => $"card#{Value}";
}
