namespace Cardgame.TestSupport;

using Cardgame.Core.Design;

/// <summary>
/// Fixture card/ability designs for tests. Deliberately not Rogue or Wizard:
/// real card content is G02 scope (ROADMAP.md). G00's own tests only need to
/// prove the design pipeline and match setup work, not carry real balance
/// data - the names below make that obvious rather than implying otherwise.
/// </summary>
public static class TestCardDesigns
{
    public const string AbilityNameKey = "test-ability";
    public const string CardAId = "test-card-a";
    public const string CardBId = "test-card-b";

    public static CardCatalog BuildCatalog()
    {
        var abilityJson = $$"""
        {
            "schema_version": 1,
            "name_key": "{{AbilityNameKey}}"
        }
        """;

        var cardAJson = $$"""
        {
            "schema_version": 1,
            "id": "{{CardAId}}",
            "type": null,
            "ability_name_keys": ["{{AbilityNameKey}}"],
            "tiers": [
                { "cost": 1, "bounty": 1, "attack": 1, "health": 1 },
                { "cost": 2, "bounty": 2, "attack": 2, "health": 2 },
                { "cost": 3, "bounty": 3, "attack": 3, "health": 3 }
            ]
        }
        """;

        var cardBJson = $$"""
        {
            "schema_version": 1,
            "id": "{{CardBId}}",
            "type": "goblin",
            "ability_name_keys": [],
            "tiers": [
                { "cost": 1, "bounty": 0, "attack": 2, "health": 1 },
                { "cost": 2, "bounty": 1, "attack": 3, "health": 2 },
                { "cost": 4, "bounty": 2, "attack": 5, "health": 3 }
            ]
        }
        """;

        return DesignCatalogLoader.LoadFromSources(
            new Dictionary<string, string>
            {
                ["card-a.json"] = cardAJson,
                ["card-b.json"] = cardBJson,
            },
            new Dictionary<string, string>
            {
                ["test-ability.json"] = abilityJson,
            });
    }

    public static IReadOnlyList<string> BuildDeck(int cardCount)
    {
        var deck = new List<string>();
        for (int i = 0; i < cardCount; i++)
        {
            deck.Add(i % 2 == 0 ? CardAId : CardBId);
        }

        return deck;
    }
}
