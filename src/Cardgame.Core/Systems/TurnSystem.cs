namespace Cardgame.Core.Systems;

using Cardgame.Core.Commands;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Zones;

/// <summary>
/// The round loop (§6, G04, G06). Turns alternate; a round is both players
/// having had one. At the start of a turn the active player draws one card
/// unless their hand is at the limit or their deck is empty (G04-02).
///
/// Mana and time are refilled when a player *ends* their turn (G06-02): the
/// maximum in the mirror modes, the round of their next turn up to the
/// maximum in Constructed, unspent mana lost either way. Both totems
/// therefore stand full through the opponent's turn, which is what makes
/// attacking them worth anything. Eight rounds without damage to a Totem of
/// Life end the match in a draw (§5.1).
/// </summary>
public static class TurnSystem
{
    /// <summary>Rounds without damage to a Totem of Life that end in a draw (G06-05).</summary>
    public const int DrawAfterQuietRounds = 8;

    public static IEvent Apply(WorldState world, EndTurnCommand command)
    {
        if (world.Outcome is not null)
        {
            return Rejected(command, "the match is over");
        }

        if (world.Turn.Round == 0)
        {
            return Rejected(command, "the match has not started");
        }

        if (command.Player != world.Turn.ActivePlayer)
        {
            return Rejected(command, $"it is {world.Turn.ActivePlayer}'s turn");
        }

        Refill(world, command.Player);

        var next = PlayerIds.Opponent(command.Player);
        if (next == world.Turn.StartingPlayer)
        {
            if (world.Turn.Round - world.Turn.LastLifeDamageRound >= DrawAfterQuietRounds)
            {
                var outcome = new MatchOutcome(
                    null, $"{DrawAfterQuietRounds} rounds without damage to a Totem of Life");
                world.Outcome = outcome;
                return new MatchEndedEvent { Outcome = outcome };
            }

            world.Turn.Round++;
        }

        return BeginTurn(world, next) with { Previous = command.Player, PreviousTimedOut = command.TimedOut };
    }

    internal static TurnStartedEvent BeginTurn(WorldState world, PlayerId player)
    {
        world.Turn.ActivePlayer = player;
        foreach (var id in world.Board.Side(player).UnitSlots)
        {
            if (id is { } unit)
            {
                world.Board.Units[unit].Ready = true;
                world.Board.Units[unit].HasAttacked = false;
            }
        }

        var zones = world.Zones(player);
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

    /// <summary>
    /// Refills a player's mana and time as their turn ends, for the turn they
    /// will have next: in Constructed that is the round after this one, since
    /// both players' next turns fall in it (G06-02).
    /// </summary>
    internal static void Refill(WorldState world, PlayerId player)
    {
        var zones = world.Zones(player);
        zones.Mana.RefillTo(world.MatchMode.IsMirror()
            ? zones.Mana.Maximum
            : Math.Min(world.Turn.Round + 1, zones.Mana.Maximum));
        zones.Time.Refill();
    }

    private static CommandRejectedEvent Rejected(ICommand command, string reason) =>
        new() { Command = command, Reason = reason };
}
