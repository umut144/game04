namespace Cardgame.Core.Zones;

/// <summary>
/// A player's mana (§6). The round loop sets it at the start of each of the
/// player's turns (<see cref="Systems.TurnSystem"/>); playing cards spends it.
/// </summary>
public sealed class ManaPool
{
    public const int StandardMaximum = 7;

    public int Maximum { get; } = StandardMaximum;
    public int Current { get; private set; }

    public bool CanPay(int cost) => cost >= 0 && cost <= Current;

    public void Pay(int cost)
    {
        if (!CanPay(cost))
        {
            throw new InvalidOperationException($"cannot pay {cost} mana with {Current}");
        }

        Current -= cost;
    }

    public void Refill() => Current = Maximum;

    public void Gain(int mana) => Set(Current + mana);

    public void Set(int mana) => Current = Math.Clamp(mana, 0, Maximum);
}
