namespace Cardgame.TestSupport;

using Cardgame.Core.Design;

/// <summary>
/// Fixture cards for combat tests: one per attack pattern, every tier the
/// same except where a test needs otherwise, all free to play.
/// </summary>
public static class CombatCards
{
    public static CardCatalog BuildCatalog()
    {
        string Card(string id, string type, string attack, int atk, int hp, int bounty, string tier3Abilities = "")
        {
            string tier = $$"""{ "cost": 0, "bounty": {{bounty}}, "attack": {{atk}}, "health": {{hp}} }""";
            string tier3 = $$"""{ "cost": 0, "bounty": {{bounty}}, "attack": {{atk}}, "health": {{hp}}, "ability_name_keys": [{{tier3Abilities}}] }""";
            return $$"""
            {
                "schema_version": 1,
                "id": "{{id}}",
                "type": {{type}},
                "ability_name_keys": [],
                "attack": {{attack}},
                "tiers": [{{tier}}, {{tier}}, {{tier3}}]
            }
            """;
        }

        var cards = new Dictionary<string, string>
        {
            ["striker"] = Card("striker", "null", """{"range": 1, "pattern": "single"}""", 3, 4, 2, "\"rush\""),
            ["arcanist"] = Card("arcanist", "\"arcane\"", """{"range": 2, "pattern": "single"}""", 3, 4, 1),
            ["doubler"] = Card("doubler", "\"arcane\"", """{"range": 3, "pattern": "double_hit"}""", 2, 4, 1),
            ["area"] = Card("area", "\"arcane\"", """{"range": 2, "pattern": "area_three"}""", 2, 4, 1),
            ["cleaver"] = Card("cleaver", "\"human\"", """{"range": 0, "pattern": "cleave"}""", 3, 5, 1),
            ["splasher"] = Card("splasher", "\"human\"", """{"range": 0, "pattern": "splash"}""", 2, 5, 1),
            ["storm"] = Card("storm", "\"arcane\"", """{"pattern": "both_front_rows"}""", 1, 5, 1),
            ["wall"] = Card("wall", "null", "null", 0, 3, 3),
            ["tough"] = Card("tough", "null", "null", 0, 9, 4),
        };

        return DesignCatalogLoader.LoadFromSources(
            cards.ToDictionary(pair => pair.Key + ".json", pair => pair.Value),
            new Dictionary<string, string> { ["rush.json"] = """{"schema_version": 1, "name_key": "rush"}""" });
    }
}
