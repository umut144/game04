using System.Collections.Generic;
using System.IO;
using Cardgame.Assets;
using Cardgame.Core.Model;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// The PolyTools assets the board draws, built once with game04's scale and
/// colours from design/asset_presentation.json.
///
/// Dev bootstrap: the files are read from disk through the project folder —
/// the synced assets under res://assets/polytools and design/ two levels up.
/// That works in the editor, not in an exported build; packing them is a
/// later concern.
/// </summary>
public sealed class BoardAssets
{
    public const string CardKey = "card";

    private readonly Dictionary<string, (AssetGeometry Geometry, Color Fill, Color Stroke)> _assets = new();

    private BoardAssets()
    {
    }

    public static BoardAssets Load()
    {
        string project = ProjectSettings.GlobalizePath("res://");
        string design = Path.GetFullPath(Path.Combine(project, "..", "..", "design"));
        var library = new SyncedAssetLibrary(ProjectSettings.GlobalizePath("res://assets/polytools"));
        string presentationPath = Path.Combine(design, "asset_presentation.json");
        var presentation = AssetPresentationLoader.Parse(File.ReadAllText(presentationPath), presentationPath);

        var assets = new BoardAssets();
        foreach (var (key, entry) in presentation)
        {
            assets._assets[key] = (library.Build(key, entry), Color.FromHtml(entry.Fill), Color.FromHtml(entry.Stroke));
        }

        return assets;
    }

    public static string KeyOf(TotemType type) => type switch
    {
        TotemType.Life => "totem_of_life",
        TotemType.Mana => "totem_of_mana",
        TotemType.Time => "totem_of_time",
        _ => throw new System.ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>The card's width in game04 meters: one card fills one column.</summary>
    public float CardWidth => _assets[CardKey].Geometry.Width;

    public void ShowIn(AssetView view, string key)
    {
        if (!_assets.TryGetValue(key, out var asset))
        {
            throw new KeyNotFoundException($"design/asset_presentation.json has no entry for '{key}'");
        }

        view.Display(asset.Geometry, asset.Fill, asset.Stroke);
    }
}
