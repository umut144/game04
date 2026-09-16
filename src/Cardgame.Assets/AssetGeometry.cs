namespace Cardgame.Assets;

/// <summary>What a triangle list of an <see cref="AssetGeometry"/> draws.</summary>
public enum AssetPartKind
{
    Fill,
    Stroke,
}

/// <summary>
/// One component's fill or stroke, as triangles in game04 meters.
/// <paramref name="Source"/> is the asset the component belongs to (a
/// referenced asset keeps its own key), <paramref name="Layer"/> a label the
/// consumer gave the geometry when combining several.
/// </summary>
/// <param name="Vertices">x0, y0, x1, y1, …</param>
public sealed record AssetPart(
    string Source,
    string ComponentName,
    AssetPartKind Kind,
    float[] Vertices,
    int[] Indices,
    string Layer = "");

/// <summary>
/// A PolyTools asset as game04 draws it: flat triangle lists in draw order,
/// in meters, y up, with the asset pivot at the origin and game04's scale
/// constants already applied.
///
/// A component's world transform is its parents' transforms, outermost first,
/// then its own position, rotation and scale; the exported vertices are
/// already pivot-relative (the contract's <c>component_transform</c>). The
/// asset pivot is subtracted last. Components are drawn in manifest order,
/// which PolyTools sorts by (z_index, component_id); each component's fill
/// comes before its stroke. An asset reference draws the referenced asset,
/// built around its own pivot, placed by the reference's world transform.
/// </summary>
public sealed class AssetGeometry
{
    private AssetGeometry(string assetKey, IReadOnlyList<AssetPart> parts, AssetGeometry? boundsFrom = null)
    {
        AssetKey = assetKey;
        Parts = parts;
        if (boundsFrom is not null)
        {
            (MinX, MaxX, MinY, MaxY) = (boundsFrom.MinX, boundsFrom.MaxX, boundsFrom.MinY, boundsFrom.MaxY);
            return;
        }

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
    public float CenterX => (MinX + MaxX) / 2f;
    public float CenterY => (MinY + MaxY) / 2f;

    /// <summary>
    /// Where a component's local (0, 0) lands, in unscaled asset meters —
    /// what PolyTools exports as <c>component_pivot</c>.
    /// </summary>
    public static (double X, double Y) OriginOf(PolyToolsManifest manifest, ManifestComponent component)
    {
        var world = WorldTransforms(manifest)[component.ComponentId];
        var (x, y) = world.Apply(0, 0);
        return (x - manifest.AssetPivot[0], y - manifest.AssetPivot[1]);
    }

    /// <param name="resolve">Finds a referenced asset's manifest by key; required only if the asset has references.</param>
    public static AssetGeometry Build(
        PolyToolsManifest manifest,
        double scaleX,
        double scaleY,
        Func<string, PolyToolsManifest>? resolve = null) =>
        Build(manifest, Affine2.FromTransform(0, 0, 0, scaleX, scaleY), resolve, new HashSet<string>());

    private static AssetGeometry Build(
        PolyToolsManifest manifest,
        Affine2 outer,
        Func<string, PolyToolsManifest>? resolve,
        HashSet<string> visiting)
    {
        if (!visiting.Add(manifest.AssetKey))
        {
            throw new ManifestException($"asset '{manifest.AssetKey}' references itself through its references");
        }

        var worlds = WorldTransforms(manifest);
        var unpivot = Affine2.FromTransform(-manifest.AssetPivot[0], -manifest.AssetPivot[1], 0, 1, 1);
        var toGame = outer.Then(unpivot);
        var parts = new List<AssetPart>();

        foreach (var component in manifest.Components)
        {
            var world = toGame.Then(worlds[component.ComponentId]);
            if (component.IsReference)
            {
                if (resolve is null)
                {
                    throw new ManifestException(
                        $"asset '{manifest.AssetKey}' references '{component.SourceAssetKey}', but no resolver was given");
                }

                var referenced = Build(resolve(component.SourceAssetKey!), world, resolve, visiting);
                parts.AddRange(referenced.Parts);
                continue;
            }

            Add(parts, manifest.AssetKey, component, AssetPartKind.Fill, component.Mesh ?? component.ClosedRegionMesh, world);
            Add(parts, manifest.AssetKey, component, AssetPartKind.Stroke, component.ContourStrokeMesh, world);
        }

        visiting.Remove(manifest.AssetKey);
        return new AssetGeometry(manifest.AssetKey, parts);
    }

    /// <summary>This geometry scaled uniformly about the origin, then moved, with every part labelled.</summary>
    public AssetGeometry Placed(float scale, float offsetX, float offsetY, string layer) =>
        new(AssetKey, Parts.Select(part => part with
        {
            Vertices = part.Vertices.Select((value, i) => value * scale + (i % 2 == 0 ? offsetX : offsetY)).ToArray(),
            Layer = layer,
        }).ToArray());

    /// <summary>The parts <paramref name="keep"/> selects, with bounds of their own.</summary>
    public AssetGeometry Where(Func<AssetPart, bool> keep) => new(AssetKey, Parts.Where(keep).ToArray());

    /// <summary>One geometry drawing <paramref name="layers"/> in order; its bounds are the first layer's.</summary>
    public static AssetGeometry Combine(params AssetGeometry[] layers) =>
        new(layers[0].AssetKey, layers.SelectMany(layer => layer.Parts).ToArray(), layers[0]);

    private static void Add(
        List<AssetPart> parts,
        string source,
        ManifestComponent component,
        AssetPartKind kind,
        ManifestMesh? mesh,
        Affine2 world)
    {
        if (mesh is null || mesh.Indices.Length == 0)
        {
            return;
        }

        var vertices = new float[mesh.Vertices.Length * 2];
        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            var (x, y) = world.Apply(mesh.Vertices[i][0], mesh.Vertices[i][1]);
            vertices[i * 2] = (float)x;
            vertices[i * 2 + 1] = (float)y;
        }

        parts.Add(new AssetPart(source, component.Name, kind, vertices, mesh.Indices.ToArray()));
    }

    private static Dictionary<string, Affine2> WorldTransforms(PolyToolsManifest manifest)
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

        foreach (var component in manifest.Components)
        {
            World(component);
        }

        return worlds;
    }
}
