namespace Cardgame.Assets;

/// <summary>What a triangle list of an <see cref="AssetGeometry"/> draws.</summary>
public enum AssetPartKind
{
    Fill,
    Stroke,
}

/// <summary>One component's fill or stroke, as triangles in game04 meters.</summary>
/// <param name="Vertices">x0, y0, x1, y1, …</param>
public sealed record AssetPart(string ComponentName, AssetPartKind Kind, float[] Vertices, int[] Indices);

/// <summary>
/// A PolyTools asset as game04 draws it: flat triangle lists in draw order,
/// in meters, y up, with the asset pivot at the origin and game04's scale
/// constants already applied.
///
/// A component's world transform is its parents' transforms, outermost first,
/// then its own position, rotation and scale. The exported vertices already
/// have the component pivot taken off, so it is not subtracted again: doing so
/// would move the card's corner glyphs to its centre. The asset pivot is
/// subtracted last. Components are drawn in manifest order, which PolyTools
/// sorts by (z_index, component_id); each component's fill comes before its
/// stroke.
/// </summary>
public sealed class AssetGeometry
{
    private AssetGeometry(string assetKey, IReadOnlyList<AssetPart> parts)
    {
        AssetKey = assetKey;
        Parts = parts;

        var fills = parts.Where(part => part.Kind == AssetPartKind.Fill).ToArray();
        var source = fills.Length > 0 ? fills : parts.ToArray();
        MinX = MinY = float.PositiveInfinity;
        MaxX = MaxY = float.NegativeInfinity;
        foreach (var part in source)
        {
            for (int i = 0; i < part.Vertices.Length; i += 2)
            {
                MinX = Math.Min(MinX, part.Vertices[i]);
                MaxX = Math.Max(MaxX, part.Vertices[i]);
                MinY = Math.Min(MinY, part.Vertices[i + 1]);
                MaxY = Math.Max(MaxY, part.Vertices[i + 1]);
            }
        }
    }

    public string AssetKey { get; }
    public IReadOnlyList<AssetPart> Parts { get; }

    /// <summary>Bounds of the fills — the asset's footprint, strokes left out.</summary>
    public float MinX { get; }
    public float MaxX { get; }
    public float MinY { get; }
    public float MaxY { get; }
    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;

    public static AssetGeometry Build(PolyToolsManifest manifest, double scaleX, double scaleY)
    {
        var byId = manifest.Components.ToDictionary(component => component.ComponentId);
        var worlds = new Dictionary<string, Affine2>();

        Affine2 World(ManifestComponent component)
        {
            if (worlds.TryGetValue(component.ComponentId, out var cached))
            {
                return cached;
            }

            var t = component.LocalTransform;
            var local = Affine2.FromTransform(t.Position[0], t.Position[1], t.RotationRadians, t.Scale[0], t.Scale[1]);
            var world = component.ParentComponentId is { } parent ? World(byId[parent]).Then(local) : local;
            worlds[component.ComponentId] = world;
            return world;
        }

        double pivotX = manifest.AssetPivot[0];
        double pivotY = manifest.AssetPivot[1];
        var parts = new List<AssetPart>();

        void Add(ManifestComponent component, AssetPartKind kind, ManifestMesh? mesh)
        {
            if (mesh is null || mesh.Indices.Length == 0)
            {
                return;
            }

            var world = World(component);
            var vertices = new float[mesh.Vertices.Length * 2];
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                var (x, y) = world.Apply(mesh.Vertices[i][0], mesh.Vertices[i][1]);
                vertices[i * 2] = (float)((x - pivotX) * scaleX);
                vertices[i * 2 + 1] = (float)((y - pivotY) * scaleY);
            }

            parts.Add(new AssetPart(component.Name, kind, vertices, mesh.Indices.ToArray()));
        }

        foreach (var component in manifest.Components)
        {
            Add(component, AssetPartKind.Fill, component.Mesh ?? component.ClosedRegionMesh);
            Add(component, AssetPartKind.Stroke, component.ContourStrokeMesh);
        }

        return new AssetGeometry(manifest.AssetKey, parts);
    }
}
