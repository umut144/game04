namespace Cardgame.Assets;

using System.Text.Json;

/// <summary>
/// The part of a PolyTools runtime manifest game04 draws: components with
/// their hierarchy, transforms and meshes. The shape is PolyTools's
/// (docs/RUNTIME_EXPORT_CONTRACT.md there); only schema 23 is accepted.
/// </summary>
public sealed record PolyToolsManifest
{
    public const int SupportedSchemaVersion = 23;

    public required int SchemaVersion { get; init; }
    public required string AssetKey { get; init; }
    public required double[] AssetPivot { get; init; }
    public required IReadOnlyList<ManifestComponent> Components { get; init; }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>Parses and validates one manifest; throws <see cref="ManifestException"/>.</summary>
    public static PolyToolsManifest Parse(string json, string source)
    {
        PolyToolsManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<PolyToolsManifest>(json, Options);
        }
        catch (JsonException exception)
        {
            throw new ManifestException($"{source}: not a readable manifest ({exception.Message})");
        }

        if (manifest is null)
        {
            throw new ManifestException($"{source}: empty manifest");
        }

        manifest.Validate(source);
        return manifest;
    }

    private void Validate(string source)
    {
        void Fail(string message) => throw new ManifestException($"{source}: {message}");

        if (SchemaVersion != SupportedSchemaVersion)
        {
            Fail($"schema {SchemaVersion}, game04 reads {SupportedSchemaVersion}");
        }

        if (!IsFinitePair(AssetPivot))
        {
            Fail("asset_pivot is not two finite numbers");
        }

        var ids = new HashSet<string>();
        foreach (var component in Components)
        {
            if (!ids.Add(component.ComponentId))
            {
                Fail($"component {component.ComponentId} appears twice");
            }
        }

        foreach (var component in Components)
        {
            string where = $"component {component.Name}";
            if (component.Kind is not null)
            {
                Fail($"{where} is a {component.Kind}, which game04 does not draw yet");
            }

            if (component.ParentComponentId is { } parent && !ids.Contains(parent))
            {
                Fail($"{where} names a missing parent {parent}");
            }

            var transform = component.LocalTransform;
            if (!IsFinitePair(transform.Position) || !IsFinitePair(transform.Scale)
                || !double.IsFinite(transform.RotationRadians)
                || transform.Scale[0] == 0 || transform.Scale[1] == 0)
            {
                Fail($"{where} has an unusable local_transform");
            }

            component.Mesh?.Validate(source, $"{where} mesh");
            component.ClosedRegionMesh?.Validate(source, $"{where} closed_region_mesh");
            component.ContourStrokeMesh?.Validate(source, $"{where} contour_stroke_mesh");
        }

        var byId = Components.ToDictionary(component => component.ComponentId);
        foreach (var component in Components)
        {
            var seen = new HashSet<string>();
            for (var current = component; current.ParentComponentId is { } parent; current = byId[parent])
            {
                if (!seen.Add(current.ComponentId))
                {
                    Fail($"component {component.Name} sits in a parent cycle");
                }
            }
        }
    }

    internal static bool IsFinitePair(double[]? pair) =>
        pair is { Length: 2 } && double.IsFinite(pair[0]) && double.IsFinite(pair[1]);
}

public sealed record ManifestComponent
{
    public required string ComponentId { get; init; }
    public required string Name { get; init; }
    public string? Kind { get; init; }
    public string? ParentComponentId { get; init; }
    public int ZIndex { get; init; }
    public required ManifestTransform LocalTransform { get; init; }
    public ManifestMesh? Mesh { get; init; }
    public ManifestMesh? ClosedRegionMesh { get; init; }
    public ManifestMesh? ContourStrokeMesh { get; init; }
}

public sealed record ManifestTransform
{
    public required double[] Position { get; init; }
    public required double RotationRadians { get; init; }
    public required double[] Scale { get; init; }
}

public sealed record ManifestMesh
{
    public required double[][] Vertices { get; init; }
    public required int[] Indices { get; init; }

    internal void Validate(string source, string where)
    {
        if (Indices.Length % 3 != 0)
        {
            throw new ManifestException($"{source}: {where} has {Indices.Length} indices, not whole triangles");
        }

        if (Vertices.Any(vertex => !PolyToolsManifest.IsFinitePair(vertex)))
        {
            throw new ManifestException($"{source}: {where} has a vertex that is not two finite numbers");
        }

        if (Indices.Any(index => index < 0 || index >= Vertices.Length))
        {
            throw new ManifestException($"{source}: {where} indexes past its {Vertices.Length} vertices");
        }
    }
}
