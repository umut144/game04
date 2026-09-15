namespace Cardgame.Assets.Tests;

using System.Text.Json;
using Xunit;

/// <summary>
/// The real files: design/asset_presentation.json against the synced
/// PolyTools assets, and the board footprints docs/BOARD_DESIGN.md relies on.
/// </summary>
public sealed class RepositoryAssetsTests
{
    private static readonly string Root = FindRepositoryRoot();
    private static readonly SyncedAssetLibrary Library =
        new(Path.Combine(Root, "src", "Cardgame.Client", "assets", "polytools"));
    private static readonly IReadOnlyDictionary<string, AssetPresentation> Presentation =
        AssetPresentationLoader.Parse(
            File.ReadAllText(Path.Combine(Root, "design", "asset_presentation.json")),
            "design/asset_presentation.json");

    [Fact]
    public void EveryKeyGame04UsesHasAPresentationAndASyncedManifest()
    {
        using var keys = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design", "asset_keys.json")));
        var used = keys.RootElement.GetProperty("asset_keys").EnumerateArray().Select(key => key.GetString()!).ToArray();

        Assert.Equal(used.OrderBy(key => key), Presentation.Keys.OrderBy(key => key));
        foreach (string key in used)
        {
            Assert.NotEmpty(Library.Build(key, Presentation[key]).Parts);
        }
    }

    [Fact]
    public void TheCardBecomesA672By960MillimetreCardStandingOnItsPivot()
    {
        var card = Library.Build("card", Presentation["card"]);

        Assert.Equal(0.672f, card.Width, 4);
        Assert.Equal(0.96f, card.Height, 4);
        Assert.Equal(0f, card.MinY, 4);
        Assert.Equal(-card.MaxX, card.MinX, 4);
        Assert.Equal(7f / 10f, card.Width / card.Height, 4);
    }

    [Fact]
    public void TheCardsCornerGlyphsSitInItsCorners()
    {
        var card = Library.Build("card", Presentation["card"]);
        var glyph = card.Parts.First(part => part.ComponentName == "mana_glyph01" && part.Kind == AssetPartKind.Fill);

        float x = glyph.Vertices.Where((_, i) => i % 2 == 0).Average();
        float y = glyph.Vertices.Where((_, i) => i % 2 == 1).Average();
        Assert.True(x < card.MinX + card.Width / 4, $"mana glyph at x={x}");
        Assert.True(y > card.MaxY - card.Height / 4, $"mana glyph at y={y}");
    }

    [Theory]
    [InlineData("totem_of_life")]
    [InlineData("totem_of_mana")]
    [InlineData("totem_of_time")]
    public void EveryTotemIs72CentimetresTallAndFitsIts90CentimetreFootprint(string key)
    {
        var totem = Library.Build(key, Presentation[key]);

        Assert.Equal(0.72f, totem.Height, 4);
        Assert.Equal(0f, totem.MinY, 4);
        Assert.True(totem.Width <= 0.9f, $"{key} is {totem.Width} m wide");
        Assert.Equal(-totem.MaxX, totem.MinX, 3);
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
