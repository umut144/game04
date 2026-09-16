namespace Cardgame.Core.Systems;

using Cardgame.Core.Commands;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Zones;

/// <summary>
/// The round loop (§6, G04). Turns alternate; a round is both players having
/// had one. At the start of a turn the active player's mana is set — the
/// maximum in the mirror modes, the round number up to the maximum in
/// Constructed, unspent mana lost either way (G04-01) — and they draw one card
/// unless their hand is at the limit or their deck is empty (G04-02).
/// </summary>
public static class TurnSystem
{
    public static IEvent Apply(WorldState world, EndTurnCommand command)
    {
        if (world.Turn.Round == 0)
        {
            return Rejected(command, "the match has not started");
        }

        if (command.Player != world.Turn.ActivePlayer)
        {
            return Rejected(command, $"it is {world.Turn.ActivePlayer}'s turn");
        }

        var next = PlayerIds.Opponent(command.Player);
        if (next == world.Turn.StartingPlayer)
        {
            world.Turn.Round++;
        }

        return BeginTurn(world, next) with { Previous = command.Player, PreviousTimedOut = command.TimedOut };
    }

    internal static TurnStartedEvent BeginTurn(WorldState world, PlayerId player)
    {
        world.Turn.ActivePlayer = player;
        var zones = world.Zones(player);
        int mana = world.MatchMode.IsMirror()
            ? zones.Mana.Maximum
            : Math.Min(world.Turn.Round, zones.Mana.Maximum);
        zones.Mana.Set(mana);

        CardInstance? drawn = null;
        if (zones.Hand.Cards.Count < PlayerZones.HandLimit)
        {
            drawn = zones.Deck.TakeTop();
            if (drawn is not null)
            {
                zones.Hand.Add(drawn);
            }
        }

        return new TurnStartedEvent
        {
            Player = player,
            Round = world.Turn.Round,
            Mana = zones.Mana.Current,
            Drawn = drawn?.Id,
        };
    }

    private static CommandRejectedEvent Rejected(ICommand command, string reason) =>
        new() { Command = command, Reason = reason };
}
