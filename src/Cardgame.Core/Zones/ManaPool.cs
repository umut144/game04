namespace Cardgame.Core.Zones;

/// <summary>
/// A player's mana and their Totem of Mana (§5.2, §6): 10 segments, one mana
/// each. The pool is refilled when its owner ends their turn (G06-02), so it
/// stands full through the opponent's turn and can be attacked there. A hit
/// takes one mana off the top; a hit on an empty pool leaves a
/// <see cref="Debt"/> segment instead, which is missing from the next refill
/// and then paid off.
/// </summary>
public sealed class ManaPool
{
    /// <summary>One per segment of the authored totem (SYNC-02).</summary>
    public const int StandardMaximum = 10;

    public int Maximum { get; } = StandardMaximum;
    public int Current { get; private set; }

    /// <summary>Mana owed: the red segments, missing from the next refill only.</summary>
    public int Debt { get; private set; }

    public bool CanPay(int cost) => cost >= 0 && cost <= Current;

    public void Pay(int cost)
    {
        if (!CanPay(cost))
        {
            throw new InvalidOperationException($"cannot pay {cost} mana with {Current}");
        }

        Current -= cost;
    }

    public void Refill() => RefillTo(Maximum);

    /// <summary>Sets the pool for a new turn, minus what is owed; the debt is then cleared.</summary>
    public void RefillTo(int mana)
    {
        Current = Math.Clamp(mana - Debt, 0, Maximum);
        Debt = 0;
    }

    /// <summary>One hit on the Totem of Mana: one mana, or one more owed.</summary>
    public void TakeHit()
    {
        if (Current > 0)
        {
            Current--;
        }
        else
        {
            Debt = Math.Min(Maximum, Debt + 1);
        }
    }

    public void Gain(int mana) => Set(Current + mana);

    public void Set(int mana) => Current = Math.Clamp(mana, 0, Maximum);
}
