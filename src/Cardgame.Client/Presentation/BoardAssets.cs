using System.Collections.Generic;
using System.IO;
using Cardgame.Assets;
using Cardgame.Core.Design;
using Cardgame.Core.Model;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// Everything the board reads from disk: the card catalog and starter deck
/// from design/, and the PolyTools art built with game04's scale and colours
/// (design/asset_presentation.json). A card's art is the card prop with its
/// character fitted inside (<see cref="CardFigureFit"/>).
///
/// Dev bootstrap: the files are read through the project folder — the synced
/// assets under res://assets/polytools and design/ two levels up. That works
/// in the editor, not in an exported build; packing them is a later concern.
/// </summary>
public sealed class BoardAssets
{
    public const string CardKey = "card";
    public const string CardLayer = "card";
    public const string FigureLayer = "figure";

    private readonly SyncedAssetLibrary _library;
    private readonly Dictionary<string, AssetGeometry> _cardArt = new();
    private readonly Dictionary<string, (AssetGeometry Geometry, Color Fill, Color Stroke)> _assets = new();

    private BoardAssets(string design, SyncedAssetLibrary library, AssetPresentationFile presentation)
    {
        _library = library;
        Catalog = DesignCatalogLoader.LoadFromDirectory(design);
        StarterDeck = DeckLoader.LoadFromFile(Path.Combine(design, "decks", "starter.json"), Catalog);
        CellFill = (float)presentation.CellFill;
        FigureFill = Color.FromHtml(presentation.FigureFill);
        FigureStroke = Color.FromHtml(presentation.FigureStroke);
        foreach (var (key, entry) in presentation.Assets)
        {
            _assets[key] = (library.Build(key, entry), Color.FromHtml(entry.Fill), Color.FromHtml(entry.Stroke));
        }

        foreach (var card in Catalog.CardsById.Values)
        {
            CardArt(card.Id);
        }
    }

    public CardCatalog Catalog { get; }
    public IReadOnlyList<string> StarterDeck { get; }

    /// <summary>How much of its cell a card or totem fills (design/asset_presentation.json).</summary>
    public float CellFill { get; }

    public Color FigureFill { get; }
    public Color FigureStroke { get; }

    public AssetGeometry Card => _assets[CardKey].Geometry;
    public Color CardFill => _assets[CardKey].Fill;
    public Color CardStroke => _assets[CardKey].Stroke;

    /// <summary>The card's size in game04 meters: one card fills one column.</summary>
    public float CardWidth => Card.Width;
    public float CardHeight => Card.Height;

    public static BoardAssets Load()
    {
        string project = ProjectSettings.GlobalizePath("res://");
        string design = Path.GetFullPath(Path.Combine(project, "..", "..", "design"));
        var library = new SyncedAssetLibrary(ProjectSettings.GlobalizePath("res://assets/polytools"));
        string presentationPath = Path.Combine(design, "asset_presentation.json");
        var presentation = AssetPresentationLoader.Parse(File.ReadAllText(presentationPath), presentationPath);
        return new BoardAssets(design, library, presentation);
    }

    public static string KeyOf(TotemType type) => type switch
    {
        TotemType.Life => "totem_of_life",
        TotemType.Mana => "totem_of_mana",
        TotemType.Time => "totem_of_time",
        _ => throw new System.ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>The card prop with the card's character centred in it.</summary>
    public AssetGeometry CardArt(string definitionId)
    {
        if (_cardArt.TryGetValue(definitionId, out var art))
        {
            return art;
        }

        var definition = Catalog.CardsById[definitionId];
        var card = Card.Placed(1f, 0f, 0f, CardLayer);
        art = definition.AssetKey is { } key
            ? AssetGeometry.Combine(card, CardFigureFit.Fit(Card, _library.Build(key, 1, 1), FigureLayer))
            : card;
        _cardArt[definitionId] = art;
        return art;
    }

    public (AssetGeometry Geometry, Color Fill, Color Stroke) Totem(TotemType type)
    {
        string key = KeyOf(type);
        if (!_assets.TryGetValue(key, out var asset))
        {
            throw new KeyNotFoundException($"design/asset_presentation.json has no entry for '{key}'");
        }

        return asset;
    }
}
