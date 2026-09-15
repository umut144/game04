namespace Cardgame.Assets.Tests;

using Xunit;

public sealed class AssetPresentationTests
{
    [Fact]
    public void AnEntryCarriesItsScaleAndColours()
    {
        var entries = AssetPresentationLoader.Parse(
            """{"schema_version": 1, "assets": {"card": {"scale": [1.5, 2], "fill": "#FFFFFF", "stroke": "#00aa33"}}}""",
            "probe");

        Assert.Equal(new AssetPresentation(1.5, 2, "#FFFFFF", "#00aa33"), entries["card"]);
    }

    [Theory]
    [InlineData("""{"schema_version": 2, "assets": {}}""")]
    [InlineData("""{"schema_version": 1}""")]
    [InlineData("""{"schema_version": 1, "assets": {"card": {"scale": [0, 1], "fill": "#FFFFFF", "stroke": "#FFFFFF"}}}""")]
    [InlineData("""{"schema_version": 1, "assets": {"card": {"scale": [1], "fill": "#FFFFFF", "stroke": "#FFFFFF"}}}""")]
    [InlineData("""{"schema_version": 1, "assets": {"card": {"scale": [1, 1], "fill": "white", "stroke": "#FFFFFF"}}}""")]
    [InlineData("""{"schema_version": 1, "assets": {"card": {"scale": [1, 1], "fill": "#FFFFFF"}}}""")]
    public void ABrokenEntryIsRefused(string json)
    {
        Assert.Throws<ManifestException>(() => AssetPresentationLoader.Parse(json, "probe"));
    }
}
