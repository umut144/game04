namespace Cardgame.Core.Tests.Design;

using Cardgame.Core.Design;
using Cardgame.TestSupport;
using Xunit;

public sealed class DesignCatalogLoaderTests
{
    [Fact]
    public void LoadsCardsAndResolvesTheirAbilities()
    {
        var catalog = TestCardDesigns.BuildCatalog();

        Assert.True(catalog.CardsById.ContainsKey(TestCardDesigns.CardAId));
        Assert.True(catalog.AbilitiesByNameKey.ContainsKey(TestCardDesigns.AbilityNameKey));

        var cardA = catalog.CardsById[TestCardDesigns.CardAId];
        Assert.Equal(3, cardA.Tiers.Count);
        Assert.Equal(TestCardDesigns.AbilityNameKey, Assert.Single(cardA.AbilityNameKeys));
    }

    [Fact]
    public void RejectsACardThatReferencesAnUnknownAbility()
    {
        var cardJson = """
        {
            "schema_version": 1,
            "id": "broken-card",
            "type": null,
            "ability_name_keys": ["does-not-exist"],
            "tiers": [
                { "cost": 1, "bounty": 1, "attack": 1, "health": 1 },
                { "cost": 2, "bounty": 2, "attack": 2, "health": 2 },
                { "cost": 3, "bounty": 3, "attack": 3, "health": 3 }
            ]
        }
        """;

        var exception = Assert.Throws<DesignException>(() =>
            DesignCatalogLoader.LoadFromSources(
                new Dictionary<string, string> { ["broken-card.json"] = cardJson },
                new Dictionary<string, string>()));

        Assert.Contains("does-not-exist", exception.Message);
    }

    [Fact]
    public void RejectsACardWithoutExactlyThreeTiers()
    {
        var cardJson = """
        {
            "schema_version": 1,
            "id": "broken-card",
            "type": null,
            "ability_name_keys": [],
            "tiers": [
                { "cost": 1, "bounty": 1, "attack": 1, "health": 1 }
            ]
        }
        """;

        var exception = Assert.Throws<DesignException>(() =>
            DesignCatalogLoader.LoadFromSources(
                new Dictionary<string, string> { ["broken-card.json"] = cardJson },
                new Dictionary<string, string>()));

        Assert.Contains("exactly 3 tiers", exception.Message);
    }
}
