using System;
using System.Linq;
using Cardgame.Assets;
using Cardgame.Core.Model;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// One card on screen: its art sized to fit this control's height, the
/// health it has lost drawn as red hatching over the health wedge (G05-04), a
/// selection frame, and the debug overlay's four corner numbers.
/// </summary>
public partial class CardControl : Control
{
    private static readonly Color SelectionColour = new("E3B341");
    private static readonly Color LostHealthColour = new("D93025");
    private static readonly Shader HatchShader = GD.Load<Shader>("res://Presentation/HatchMask.gdshader");

    private readonly AssetView _art;
    private readonly AssetView _lostHealth;
    private readonly Panel _frame;
    private readonly Label[] _numbers = new Label[4];
    private BoardAssets? _assets;

    public event Action? Clicked;

    public CardControl(Func<float>? pixelsPerMeter = null)
    {
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;

        _frame = new Panel { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        var style = new StyleBoxFlat { BgColor = new Color(SelectionColour, 0.18f), BorderColor = SelectionColour };
        style.SetBorderWidthAll(4);
        _frame.AddThemeStyleboxOverride("panel", style);
        Fill(_frame);
        AddChild(_frame);

        _art = new AssetView();
        _art.PixelsPerMeter = pixelsPerMeter ?? (() => _assets is null ? 1f : Size.Y / _assets.CardHeight);
        Fill(_art);
        AddChild(_art);

        _lostHealth = new AssetView { Material = new ShaderMaterial { Shader = HatchShader } };
        _lostHealth.PixelsPerMeter = _art.PixelsPerMeter;
        Fill(_lostHealth);
        AddChild(_lostHealth);

        var corners = new[] { (0f, 0f), (1f, 0f), (0f, 1f), (1f, 1f) };
        for (int i = 0; i < 4; i++)
        {
            var label = new Label { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            label.AddThemeFontSizeOverride("font_size", 22);
            label.AddThemeColorOverride("font_color", Colors.Black);
            label.AddThemeColorOverride("font_outline_color", Colors.White);
            label.AddThemeConstantOverride("outline_size", 6);
            label.HorizontalAlignment = corners[i].Item1 < 0.5f ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            label.VerticalAlignment = corners[i].Item2 < 0.5f ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            Fill(label);
            label.OffsetLeft = 16;
            label.OffsetRight = -16;
            label.OffsetTop = 14;
            label.OffsetBottom = -14;
            _numbers[i] = label;
            AddChild(label);
        }

        GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                Clicked?.Invoke();
                AcceptEvent();
            }
        };
    }

    /// <param name="values">The tier's values; <paramref name="damage"/> is taken off its health.</param>
    public void ShowCard(BoardAssets assets, string definitionId, CardTier values, bool debug, bool selected, int damage = 0)
    {
        _assets = assets;
        int maxHealth = values.Health;
        int health = System.Math.Max(maxHealth - damage, 0);
        var shown = values with { Health = health };
        var art = assets.CardArt(definitionId);
        _art.Fill = assets.CellFill;
        _art.Display(art, part => CardColours.Of(part, shown, assets));

        _lostHealth.Fill = assets.CellFill;
        var lost = art.Where(part =>
            part.Layer == BoardAssets.CardLayer
            && part.Kind == AssetPartKind.Fill
            && GlyphNumber(part.ComponentName, "health") is int glyph
            && GlyphBands.IsFilled(System.Math.Min(maxHealth, GlyphBands.MaximumValue), glyph)
            && !GlyphBands.IsFilled(System.Math.Min(health, GlyphBands.MaximumValue), glyph));
        if (damage > 0 && lost.Parts.Count > 0)
        {
            _lostHealth.Display(lost, _ => LostHealthColour);
        }
        else
        {
            _lostHealth.Clear();
        }

        _frame.Visible = selected;
        string healthText = damage > 0 ? $"{health}/{maxHealth}" : maxHealth.ToString();
        string[] texts = { values.Cost.ToString(), values.Bounty.ToString(), values.Attack.ToString(), healthText };
        for (int i = 0; i < 4; i++)
        {
            _numbers[i].Text = texts[i];
            _numbers[i].Visible = debug;
        }

        Visible = true;
    }

    public void ShowBack(BoardAssets assets)
    {
        _assets = assets;
        _lostHealth.Clear();
        _art.Fill = assets.CellFill;
        _art.Display(assets.Card, part => CardColours.Of(part, null, assets));
        _frame.Visible = false;
        foreach (var number in _numbers)
        {
            number.Visible = false;
        }

        Visible = true;
    }

    // "health_glyph02" → 2 for corner "health"; anything else → null.
    private static int? GlyphNumber(string name, string corner) =>
        name.StartsWith(corner + "_glyph0", StringComparison.Ordinal)
        && name.Length == corner.Length + 8
        && int.TryParse(name.Substring(corner.Length + 7), out int n) && n >= 1 && n <= GlyphBands.BandCount
            ? n
            : null;

    public static void Fill(Control control)
    {
        control.AnchorLeft = 0;
        control.AnchorTop = 0;
        control.AnchorRight = 1;
        control.AnchorBottom = 1;
        control.OffsetLeft = 0;
        control.OffsetTop = 0;
        control.OffsetRight = 0;
        control.OffsetBottom = 0;
    }
}
