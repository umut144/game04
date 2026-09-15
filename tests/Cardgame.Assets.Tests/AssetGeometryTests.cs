namespace Cardgame.Assets.Tests;

using Xunit;

public sealed class AssetGeometryTests
{
    // One parent at (1,0) and one child at (0,1), turned a quarter to the left,
    // with a single triangle whose first vertex is (1,0); asset pivot (1,0).
    private const string TwoComponents = """
        {
          "schema_version": 23,
          "asset_key": "probe",
          "asset_pivot": [1.0, 0.0],
          "components": [
            {
              "component_id": "component_1",
              "name": "parent",
              "parent_component_id": null,
              "z_index": 0,
              "component_pivot": [5.0, 5.0],
              "local_transform": {"position": [1.0, 0.0], "rotation_radians": 0.0, "scale": [1.0, 1.0]},
              "mesh": {"vertices": [[0.0, 0.0], [1.0, 0.0], [0.0, 1.0]], "indices": [0, 1, 2]},
              "contour_stroke_mesh": {"vertices": [[0.0, 0.0], [1.0, 0.0], [0.0, 1.0]], "indices": [0, 1, 2]}
            },
            {
              "component_id": "component_2",
              "name": "child",
              "parent_component_id": "component_1",
              "z_index": 1,
              "component_pivot": [5.0, 5.0],
              "local_transform": {"position": [0.0, 1.0], "rotation_radians": 1.5707963267948966, "scale": [1.0, 1.0]},
              "mesh": {"vertices": [[1.0, 0.0], [2.0, 0.0], [1.0, 1.0]], "indices": [0, 1, 2]}
            }
          ]
        }
        """;

    [Fact]
    public void AChildIsPlacedThroughItsParentAndThePivotsAreApplied()
    {
        var manifest = PolyToolsManifest.Parse(TwoComponents, "probe");

        var geometry = AssetGeometry.Build(manifest, 2.0, 3.0);

        var child = geometry.Parts.Single(part => part.ComponentName == "child");
        // (1,0) turned a quarter → (0,1); + own (0,1) → (0,2); + parent (1,0)
        // → (1,2); − asset pivot (1,0) → (0,2); × scale (2,3) → (0,6). The
        // component pivot (5,5) is not subtracted.
        Assert.Equal(0f, child.Vertices[0], 5);
        Assert.Equal(6f, child.Vertices[1], 5);
    }

    [Fact]
    public void PartsFollowManifestOrderWithEachFillBeforeItsStroke()
    {
        var geometry = AssetGeometry.Build(PolyToolsManifest.Parse(TwoComponents, "probe"), 1, 1);

        Assert.Equal(
            new[] { ("parent", AssetPartKind.Fill), ("parent", AssetPartKind.Stroke), ("child", AssetPartKind.Fill) },
            geometry.Parts.Select(part => (part.ComponentName, part.Kind)));
    }

    [Theory]
    [InlineData("\"schema_version\": 23", "\"schema_version\": 22")]
    [InlineData("\"parent_component_id\": \"component_1\"", "\"parent_component_id\": \"component_9\"")]
    [InlineData("\"indices\": [0, 1, 2]}\n", "\"indices\": [0, 1, 7]}\n")]
    [InlineData("\"indices\": [0, 1, 2]}\n", "\"indices\": [0, 1]}\n")]
    [InlineData("\"name\": \"child\",", "\"name\": \"child\", \"kind\": \"asset_reference\",")]
    [InlineData("\"parent_component_id\": null", "\"parent_component_id\": \"component_2\"")]
    public void AManifestGame04CannotDrawIsRefused(string original, string broken)
    {
        Assert.Contains(original, TwoComponents);
        string json = ReplaceFirst(TwoComponents, original, broken);

        Assert.Throws<ManifestException>(() => PolyToolsManifest.Parse(json, "probe"));
    }

    private static string ReplaceFirst(string text, string original, string replacement)
    {
        int index = text.IndexOf(original, StringComparison.Ordinal);
        return text[..index] + replacement + text[(index + original.Length)..];
    }
}
