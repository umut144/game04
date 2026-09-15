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
/// both decks built and shuffled, both totem layouts rolled. The starting
/// hand is G04's (docs/TASKS.md, CORE-10).
/// </summary>
public static class MatchSetupSystem
{
    public static (WorldState World, MatchSetUpEvent Event) Apply(SetupMatchCommand command, CardCatalog catalog)
    {
        var world = new WorldState(command.Seed, command.MirrorMode);

        BuildDeck(world.PlayerA.Deck, command.PlayerADeckDefinitionIds, catalog, world.CardIds);
        BuildDeck(world.PlayerB.Deck, command.PlayerBDeckDefinitionIds, catalog, world.CardIds);

        ShuffleDeck(world.PlayerA.Deck, command.Seed, command.MirrorMode, PlayerId.PlayerA);
        ShuffleDeck(world.PlayerB.Deck, command.Seed, command.MirrorMode, PlayerId.PlayerB);

        foreach (var player in new[] { PlayerId.PlayerA, PlayerId.PlayerB })
        {
            world.Board.Side(player).SetTotemLayout(RollTotemLayout(command.Seed, command.MirrorMode, player));
        }

        var setUpEvent = new MatchSetUpEvent { Seed = command.Seed };
        return (world, setUpEvent);
    }

    /// <summary>
    /// The totems for places A, B, C of one side. Perfect Mirror: both sides
    /// use the same purpose label and so roll the same layout (§2). Shuffled
    /// Mirror: each side rolls on its own, which may by chance come out the
    /// same (1 in 6) — independent means exactly that. Its own stream, so the
    /// decks never move the totems.
    /// </summary>
    public static IReadOnlyList<TotemType> RollTotemLayout(ulong rootSeed, MirrorMode mirrorMode, PlayerId player)
    {
        var types = new List<TotemType>(Enum.GetValues<TotemType>());
        var rng = new Xoshiro256StarStar(
            SeedDerivation.DeriveSubSeed(rootSeed, PurposeFor("totem-layout", mirrorMode, player)));
        DeterministicShuffle.ShuffleInPlace(types, rng);
        return types;
    }

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

    private static void ShuffleDeck(Zone deck, ulong rootSeed, MirrorMode mirrorMode, PlayerId player)
    {
        ulong subSeed = SeedDerivation.DeriveSubSeed(rootSeed, PurposeFor("deck-order", mirrorMode, player));
        deck.ShuffleInPlace(new Xoshiro256StarStar(subSeed));
    }

    // Perfect Mirror: one label for both sides, hence the identical sub-seed.
    // Shuffled Mirror: one label per side, hence independent streams. Either
    // way there is exactly one root seed for the whole match (CORE-03).
    private static string PurposeFor(string purpose, MirrorMode mirrorMode, PlayerId player) =>
        mirrorMode == MirrorMode.PerfectMirror ? purpose : $"{purpose}:{player}";
}
