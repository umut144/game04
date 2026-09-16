namespace Cardgame.Assets;

/// <summary>
/// The PolyTools assets synced into the client (SYNC-02):
/// <c>&lt;root&gt;/&lt;type folder&gt;/&lt;asset key&gt;/manifest.json</c>. The key is
/// found by looking, not by knowing the sync script's type-to-folder names.
/// </summary>
public sealed class SyncedAssetLibrary
{
    private readonly string _root;

    public SyncedAssetLibrary(string root)
    {
        _root = root;
    }

    public string ManifestPathFor(string assetKey)
    {
        if (!Directory.Exists(_root))
        {
            throw new ManifestException($"no synced PolyTools assets at {_root}");
        }

        var matches = Directory.EnumerateDirectories(_root)
            .Select(folder => Path.Combine(folder, assetKey, "manifest.json"))
            .Where(File.Exists)
            .ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new ManifestException($"no synced PolyTools asset '{assetKey}' under {_root}"),
            _ => throw new ManifestException($"PolyTools asset '{assetKey}' is synced more than once under {_root}"),
        };
    }

    public PolyToolsManifest Load(string assetKey)
    {
        string path = ManifestPathFor(assetKey);
        return PolyToolsManifest.Parse(File.ReadAllText(path), path);
    }

    public AssetGeometry Build(string assetKey, AssetPresentation presentation) =>
        Build(assetKey, presentation.ScaleX, presentation.ScaleY);

    /// <summary>Builds an asset, resolving any asset references from this library.</summary>
    public AssetGeometry Build(string assetKey, double scaleX, double scaleY) =>
        AssetGeometry.Build(Load(assetKey), scaleX, scaleY, Load);
}
