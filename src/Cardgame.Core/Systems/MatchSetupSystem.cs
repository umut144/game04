namespace Cardgame.Core.Systems;

using Cardgame.Core.Board;
using Cardgame.Core.Commands;
using Cardgame.Core.Design;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Rng;
using Cardgame.Core.Zones;

/// <summary>
/// Turns a <see cref="SetupMatchCommand"/> into a <see cref="WorldState"/>:
/// both decks built and shuffled, both totem layouts rolled, the starting
/// hand of 3 dealt from the top of each deck, the starting player drawn from
/// the seed, and their first turn begun — which draws their fourth card
/// (G04-02, G04-03).
/// </summary>
public static class MatchSetupSystem
{
    public const int StartingHandSize = 3;

    public static (WorldState World, MatchSetUpEvent Event) Apply(SetupMatchCommand command, CardCatalog catalog)
    {
        var world = new WorldState(command.Seed, command.MatchMode);

        BuildDeck(world.PlayerA.Deck, command.PlayerADeckDefinitionIds, catalog, world.CardIds);
        BuildDeck(world.PlayerB.Deck, command.PlayerBDeckDefinitionIds, catalog, world.CardIds);

        ShuffleDeck(world.PlayerA.Deck, command.Seed, command.MatchMode, PlayerId.PlayerA);
        ShuffleDeck(world.PlayerB.Deck, command.Seed, command.MatchMode, PlayerId.PlayerB);

        foreach (var player in new[] { PlayerId.PlayerA, PlayerId.PlayerB })
        {
            world.Board.Side(player).SetTotemLayout(RollTotemLayout(command.Seed, command.MatchMode, player));
        }

        foreach (var player in new[] { PlayerId.PlayerA, PlayerId.PlayerB })
        {
            var zones = world.Zones(player);
            for (int i = 0; i < StartingHandSize && zones.Hand.Cards.Count < PlayerZones.HandLimit; i++)
            {
                if (zones.Deck.TakeTop() is not { } card)
                {
                    break;
                }

                zones.Hand.Add(card);
            }
        }

        // The first turn of each player is already paid for at setup: the
        // mirror modes start at the maximum, Constructed at the first round's
        // one mana (G04-01, G06-02).
        foreach (var player in new[] { PlayerId.PlayerA, PlayerId.PlayerB })
        {
            var zones = world.Zones(player);
            zones.Mana.RefillTo(command.MatchMode.IsMirror() ? zones.Mana.Maximum : 1);
            zones.Time.Refill();
        }

        var starter = RollStartingPlayer(command.Seed);
        world.Turn.StartingPlayer = starter;
        world.Turn.Round = 1;
        var firstTurn = TurnSystem.BeginTurn(world, starter);

        var setUpEvent = new MatchSetUpEvent { Seed = command.Seed, FirstTurn = firstTurn };
        return (world, setUpEvent);
    }

    /// <summary>
    /// The totems for places A, B, C of one side. Perfect Mirror: both sides
    /// use the same purpose label and so roll the same layout (§2). Shuffled
    /// Mirror and Constructed: each side rolls on its own, which may by chance come out the
    /// same (1 in 6) — independent means exactly that. Its own stream, so the
    /// decks never move the totems.
    /// </summary>
    public static IReadOnlyList<TotemType> RollTotemLayout(ulong rootSeed, MatchMode matchMode, PlayerId player)
    {
        var types = new List<TotemType>(Enum.GetValues<TotemType>());
        var rng = new Xoshiro256StarStar(
            SeedDerivation.DeriveSubSeed(rootSeed, PurposeFor("totem-layout", matchMode, player)));
        DeterministicShuffle.ShuffleInPlace(types, rng);
        return types;
    }

    /// <summary>
    /// Who starts: drawn from the seed for now (G04-03). Speed decides once
    /// Mastery Stats exist (§12, G09).
    /// </summary>
    public static PlayerId RollStartingPlayer(ulong rootSeed) =>
        new Xoshiro256StarStar(SeedDerivation.DeriveSubSeed(rootSeed, "starting-player")).NextInt(2) == 0
            ? PlayerId.PlayerA
            : PlayerId.PlayerB;

    private static void BuildDeck(
        Zone deck,
        IReadOnlyList<string> definitionIds,
        CardCatalog catalog,
        CardInstanceIdGenerator idGenerator)
    {
        foreach (var definitionId in definitionIds)
        {
            if (!catalog.CardsById.ContainsKey(definitionId))
            {
                throw new ArgumentException($"unknown card definition '{definitionId}'", nameof(definitionIds));
            }

            deck.Add(new CardInstance(idGenerator.Next(), definitionId));
        }
    }

    private static void ShuffleDeck(Zone deck, ulong rootSeed, MatchMode matchMode, PlayerId player)
    {
        ulong subSeed = SeedDerivation.DeriveSubSeed(rootSeed, PurposeFor("deck-order", matchMode, player));
        deck.ShuffleInPlace(new Xoshiro256StarStar(subSeed));
    }

    // Perfect Mirror: one label for both sides, hence the identical sub-seed.
    // Shuffled Mirror and Constructed: one label per side, hence independent
    // streams. Either
    // way there is exactly one root seed for the whole match (CORE-03).
    private static string PurposeFor(string purpose, MatchMode matchMode, PlayerId player) =>
        matchMode == MatchMode.PerfectMirror ? purpose : $"{purpose}:{player}";
}
