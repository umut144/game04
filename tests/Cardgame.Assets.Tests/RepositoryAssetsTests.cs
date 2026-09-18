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
            "design/asset_presentation.json").Assets;

    private static readonly string[] CardFigureKeys = Directory
        .EnumerateFiles(Path.Combine(Root, "design", "cards"), "*.json")
        .Select(path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("asset_key").GetString()!)
        .Distinct()
        .ToArray();

    public static TheoryData<string> Figures()
    {
        var data = new TheoryData<string>();
        foreach (string key in CardFigureKeys)
        {
            data.Add(key);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Figures))]
    public void EveryCardFigureFitsCentredInsideTheCardAndClearOfItsCornerWedges(string key)
    {
        var card = Library.Build("card", Presentation["card"]);
        var window = CardFigureFit.WindowOf(card);

        var figure = CardFigureFit.Fit(card, Library.Build(key, 1, 1), "figure");

        const float tolerance = 1e-4f;
        Assert.Equal(window.CenterX, figure.CenterX, 3);
        Assert.Equal(window.CenterY, figure.CenterY, 3);
        Assert.True(figure.Width <= card.Width + tolerance && figure.Height <= card.Height + tolerance, $"{key} leaves the card");
        bool clearBetweenSides = figure.Width / 2 <= window.InnerHalfWidth + tolerance;
        bool clearBetweenTopAndBottom = figure.Height / 2 <= window.InnerHalfHeight + tolerance;
        Assert.True(clearBetweenSides || clearBetweenTopAndBottom, $"{key} overlaps a corner wedge");
        bool touchesALimit = Math.Abs(figure.Width / 2 - window.InnerHalfWidth) < 1e-3
            || Math.Abs(figure.Height / 2 - window.HalfHeight) < 1e-3
            || Math.Abs(figure.Width / 2 - window.HalfWidth) < 1e-3
            || Math.Abs(figure.Height / 2 - window.InnerHalfHeight) < 1e-3;
        Assert.True(touchesALimit, $"{key} is not as large as it could be");
    }

    [Fact]
    public void TheCardWindowLiesBetweenTheCornerWedges()
    {
        var card = Library.Build("card", Presentation["card"]);

        var window = CardFigureFit.WindowOf(card);

        // PolyTools: the glyph01 wedges — the largest of the four bands since
        // VALUE-12 — end 0.2 m in from the 0.3 m half-width and 0.2 m in from
        // the 0.45 m half-height; scaled by 1.12 and 16/15.
        Assert.Equal(0.112f, window.InnerHalfWidth, 3);
        Assert.Equal(0.2667f, window.InnerHalfHeight, 3);
        Assert.Equal(0.336f, window.HalfWidth, 3);
        Assert.Equal(0.48f, window.HalfHeight, 3);
    }

    [Fact]
    public void TheBardeDrawsItsReferencedOrbAndEyes()
    {
        var barde = Library.Build("barde", 1, 1);

        Assert.Contains(barde.Parts, part => part.Source == "orb");
        var eyes = barde.Parts
            .Where(part => part.Source == "plus" && part.ComponentName == "main_vertical")
            .Select(part => MathF.Round(part.Vertices.Where((_, i) => i % 2 == 0).Average(), 3))
            .Distinct()
            .ToArray();
        Assert.Equal(2, eyes.Length);
        Assert.True(eyes.Min() < 0 && eyes.Max() > 0, $"eyes at {string.Join(", ", eyes)}");
        Assert.Contains(barde.Parts, part => part.Source == "barde");
    }

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

    [Fact]
    public void EveryComponentPivotIsWhereTheComponentsOriginLands()
    {
        // The contract's cross-check that vertices are pivot-relative: placing
        // local (0, 0) must give the exported component_pivot, for every
        // synced single.
        var root = Path.Combine(Root, "src", "Cardgame.Client", "assets", "polytools");
        var manifests = Directory.EnumerateFiles(root, "manifest.json", SearchOption.AllDirectories).ToArray();
        Assert.NotEmpty(manifests);

        int checkedCount = 0;
        foreach (string path in manifests)
        {
            string json = File.ReadAllText(path);
            var manifest = PolyToolsManifest.Parse(json, path);
            checkedCount++;
            foreach (var component in manifest.Components)
            {
                var pivot = component.ComponentPivot;
                Assert.True(pivot is { Length: 2 }, $"{manifest.AssetKey}/{component.Name} has no component_pivot");
                var (x, y) = AssetGeometry.OriginOf(manifest, component);
                Assert.True(
                    Math.Abs(x - pivot![0]) < 1e-4 && Math.Abs(y - pivot[1]) < 1e-4,
                    $"{manifest.AssetKey}/{component.Name}: origin ({x}, {y}), component_pivot ({pivot[0]}, {pivot[1]})");
            }
        }

        Assert.Equal(manifests.Length, checkedCount);
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
