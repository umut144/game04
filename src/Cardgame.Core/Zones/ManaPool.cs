namespace Cardgame.Core.Zones;

/// <summary>
/// A player's mana (§6). G02 starts everyone at the maximum of 7 and only
/// spends it; growth per round and the refill belong to G04.
/// </summary>
public sealed class ManaPool
{
    public const int StandardMaximum = 7;

    public int Maximum { get; } = StandardMaximum;
    public int Current { get; private set; } = StandardMaximum;

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
}
