namespace Cardgame.Core.Design;

using System.Text.Json;

/// <summary>
/// A deck as authored under design/decks/*.json: card ids with counts. Both
/// sides of a mirror match play the same list (§2). Deck composition as a
/// feature is G10; this file only feeds match setup until then.
/// </summary>
public static class DeckLoader
{
    public const int SupportedSchemaVersion = 1;

    private sealed record DeckDesign(int SchemaVersion, string Id, IReadOnlyList<DeckEntry> Cards);

    private sealed record DeckEntry(string Id, int Count);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>The deck's card ids in file order, each repeated by its count.</summary>
    public static IReadOnlyList<string> Parse(string json, string source, CardCatalog catalog)
    {
        DeckDesign? deck;
        try
        {
            deck = JsonSerializer.Deserialize<DeckDesign>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new DesignException($"{source}: cannot parse deck: {exception.Message}");
        }

        if (deck is null || deck.SchemaVersion != SupportedSchemaVersion || deck.Cards is null)
        {
            throw new DesignException($"{source}: not a schema {SupportedSchemaVersion} deck");
        }

        var errors = new List<string>();
        var ids = new List<string>();
        foreach (var entry in deck.Cards)
        {
            if (!catalog.CardsById.ContainsKey(entry.Id))
            {
                errors.Add($"{source}: unknown card '{entry.Id}'");
            }

            if (entry.Count < 1)
            {
                errors.Add($"{source}: card '{entry.Id}' has count {entry.Count}");
            }

            ids.AddRange(Enumerable.Repeat(entry.Id, Math.Max(entry.Count, 0)));
        }

        if (ids.Count == 0)
        {
            errors.Add($"{source}: the deck is empty");
        }

        if (errors.Count > 0)
        {
            throw new DesignException(string.Join("; ", errors));
        }

        return ids;
    }

    public static IReadOnlyList<string> LoadFromFile(string path, CardCatalog catalog) =>
        Parse(File.ReadAllText(path), path, catalog);
}
