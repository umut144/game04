namespace Cardgame.Assets.Tests;

using Xunit;

public sealed class AssetPresentationTests
{
    [Fact]
    public void AnEntryCarriesItsScaleAndColoursAndTheFileItsCellFill()
    {
        var file = AssetPresentationLoader.Parse(
            """{"schema_version": 1, "cell_fill": 0.9, "figure": {"fill": "#FFFFFF", "stroke": "#000000"}, "assets": {"card": {"scale": [1.5, 2], "fill": "#FFFFFF", "stroke": "#00aa33"}}}""",
            "probe");

        Assert.Equal(0.9, file.CellFill);
        Assert.Equal("#000000", file.FigureStroke);
        Assert.Equal(new AssetPresentation(1.5, 2, "#FFFFFF", "#00aa33"), file.Assets["card"]);
    }

    [Theory]
    [InlineData("""{"schema_version": 2, "assets": {}}""")]
    [InlineData("""{"schema_version": 1, "cell_fill": 0.96}""")]
    [InlineData("""{"schema_version": 1, "assets": {}}""")]
    [InlineData("""{"schema_version": 1, "cell_fill": 0.96, "assets": {}}""")]
    [InlineData("""{"schema_version": 1, "cell_fill": 0, "assets": {}}""")]
    [InlineData("""{"schema_version": 1, "cell_fill": 1.2, "assets": {}}""")]
    [InlineData("""{"schema_version": 1, "cell_fill": 0.96, "figure": {"fill": "#FFFFFF", "stroke": "#000000"}, "assets": {"card": {"scale": [0, 1], "fill": "#FFFFFF", "stroke": "#FFFFFF"}}}""")]
    [InlineData("""{"schema_version": 1, "cell_fill": 0.96, "figure": {"fill": "#FFFFFF", "stroke": "#000000"}, "assets": {"card": {"scale": [1], "fill": "#FFFFFF", "stroke": "#FFFFFF"}}}""")]
    [InlineData("""{"schema_version": 1, "cell_fill": 0.96, "figure": {"fill": "#FFFFFF", "stroke": "#000000"}, "assets": {"card": {"scale": [1, 1], "fill": "white", "stroke": "#FFFFFF"}}}""")]
    [InlineData("""{"schema_version": 1, "cell_fill": 0.96, "figure": {"fill": "#FFFFFF", "stroke": "#000000"}, "assets": {"card": {"scale": [1, 1], "fill": "#FFFFFF"}}}""")]
    public void ABrokenEntryIsRefused(string json)
    {
        Assert.Throws<ManifestException>(() => AssetPresentationLoader.Parse(json, "probe"));
    }
}
