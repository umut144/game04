namespace Cardgame.Core.Tests.Design;

using Cardgame.Core.Design;
using Xunit;

/// <summary>The real design/ tree loads and its starter deck is playable.</summary>
public sealed class RepositoryDesignTests
{
    private static readonly string DesignRoot = Path.Combine(FindRepositoryRoot(), "design");

    [Fact]
    public void TheDesignTreeLoadsWithEveryCardNamingItsArt()
    {
        var catalog = DesignCatalogLoader.LoadFromDirectory(DesignRoot);

        Assert.NotEmpty(catalog.CardsById);
        Assert.All(catalog.CardsById.Values, card => Assert.False(string.IsNullOrWhiteSpace(card.AssetKey)));
        Assert.Contains("rogue", catalog.CardsById.Keys);
        Assert.Contains("wizard", catalog.CardsById.Keys);
    }

    [Fact]
    public void TheStarterDeckHasTwentyKnownCards()
    {
        var catalog = DesignCatalogLoader.LoadFromDirectory(DesignRoot);

        var deck = DeckLoader.LoadFromFile(Path.Combine(DesignRoot, "decks", "starter.json"), catalog);

        Assert.Equal(20, deck.Count);
        Assert.All(deck, id => Assert.True(catalog.CardsById.ContainsKey(id)));
    }

    [Theory]
    [InlineData("{\"schema_version\": 1, \"id\": \"d\", \"cards\": [{\"id\": \"nobody\", \"count\": 1}]}")]
    [InlineData("{\"schema_version\": 1, \"id\": \"d\", \"cards\": [{\"id\": \"rogue\", \"count\": 0}]}")]
    [InlineData("{\"schema_version\": 1, \"id\": \"d\", \"cards\": []}")]
    [InlineData("{\"schema_version\": 2, \"id\": \"d\", \"cards\": [{\"id\": \"rogue\", \"count\": 1}]}")]
    public void ABrokenDeckIsRefused(string json)
    {
        var catalog = DesignCatalogLoader.LoadFromDirectory(DesignRoot);

        Assert.Throws<DesignException>(() => DeckLoader.Parse(json, "probe", catalog));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "game04.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("game04.sln not found above the test binaries");
    }
}
