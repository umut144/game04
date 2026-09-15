namespace Cardgame.Core.Systems;

using Cardgame.Core.Commands;
using Cardgame.Core.Design;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Rng;
using Cardgame.Core.Zones;

/// <summary>
/// The one system G00 needs: turns a <see cref="SetupMatchCommand"/> into a
/// <see cref="WorldState"/>. Board geometry, zones and the seeded shuffle
/// are G00 scope; totem identity assignment (G01), starting hand (G04) and
/// everything else are later gates' systems.
/// </summary>
public static class MatchSetupSystem
{
    public static (WorldState World, MatchSetUpEvent Event) Apply(SetupMatchCommand command, CardCatalog catalog)
    {
        var world = new WorldState(command.Seed, command.MirrorMode);
        var idGenerator = new CardInstanceIdGenerator();

        BuildDeck(world.PlayerA.Deck, command.PlayerADeckDefinitionIds, catalog, idGenerator);
        BuildDeck(world.PlayerB.Deck, command.PlayerBDeckDefinitionIds, catalog, idGenerator);

        ShuffleDeck(world.PlayerA.Deck, command.Seed, command.MirrorMode, PlayerId.PlayerA);
        ShuffleDeck(world.PlayerB.Deck, command.Seed, command.MirrorMode, PlayerId.PlayerB);

        var setUpEvent = new MatchSetUpEvent { Seed = command.Seed };
        return (world, setUpEvent);
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
        // Perfect Mirror: both sides draw the same purpose label, so they
        // derive the identical sub-seed and therefore the identical order.
        // Shuffled Mirror: each side gets its own label, hence its own,
        // independent stream. Either way there is exactly one root seed for
        // the whole match (CORE-03).
        string purpose = mirrorMode == MirrorMode.PerfectMirror
            ? "deck-order"
            : $"deck-order:{player}";

        ulong subSeed = SeedDerivation.DeriveSubSeed(rootSeed, purpose);
        deck.ShuffleInPlace(new Xoshiro256StarStar(subSeed));
    }
}
