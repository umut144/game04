using System.Collections.Generic;
using Cardgame.Core;
using Cardgame.Core.Board;
using Cardgame.Core.Commands;
using Cardgame.Core.Design;
using Cardgame.Core.Model;
using Cardgame.Core.Snapshot;
using Cardgame.Core.Systems;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// G01's board: the grid of <see cref="BoardLayoutSpec"/>, the rolled totems
/// and blank cards drawn with the synced PolyTools art (<see cref="BoardAssets"/>),
/// cards placed and removed by clicking a slot. It draws a <see cref="PlayerView"/> and sends commands;
/// it decides nothing itself.
///
/// Dev bootstrap: there is no server yet, so this node holds the world and
/// applies commands to it directly, always showing PlayerA's view (own side
/// at the bottom). Clicking the opponent's row places for PlayerB, so both
/// sides can be tried from one screen. R rolls a new seed, M switches the
/// mirror mode.
///
/// Both rows count 1-6 from the left and the totem places run A, B, C from
/// the left on both sides: the view is mirrored, not turned, so slot n faces
/// slot n (BoardGeometry).
/// </summary>
public partial class BoardScreen : Control
{
    private static readonly Color Background = new(0.055f, 0.09f, 0.07f);
    private static readonly Color CellFill = new(0.10f, 0.14f, 0.12f);
    private static readonly Color CellBorder = new(0.30f, 0.36f, 0.33f);
    private static readonly Color OwnSlotFill = new(0.13f, 0.19f, 0.25f);
    private static readonly Color OpponentSlotFill = new(0.22f, 0.14f, 0.17f);
    private static readonly Color MarginFill = new(0.08f, 0.11f, 0.10f);
    private static readonly Color DimText = new(0.55f, 0.60f, 0.57f);

    private readonly CardCatalog _catalog = DesignCatalogLoader.LoadFromSources(
        new Dictionary<string, string>(), new Dictionary<string, string>());

    private readonly Dictionary<(PlayerId Player, int Slot), AssetView> _cards = new();
    private readonly Dictionary<(PlayerId Player, TotemPosition Position), AssetView> _totems = new();

    private const PlayerId Viewer = PlayerId.PlayerA;

    private WorldState _world = null!;
    private ulong _seed = 1;
    private MirrorMode _mirrorMode = MirrorMode.ShuffledMirror;
    private Label _info = null!;
    private Control _root = null!;
    private BoardAssets? _assets;
    private string _assetError = string.Empty;

    public override void _Ready()
    {
        AddChild(MakeRect(Background, 0f, 0f, 1f, 1f));
        try
        {
            _assets = BoardAssets.Load();
        }
        catch (System.Exception exception)
        {
            _assetError = exception.Message;
            GD.PushError($"board art not loaded: {exception.Message}");
        }

        BuildBoard();
        NewMatch();
    }

    private void BuildBoard()
    {
        _root = MakeRect(Background, 0f, 0f, 1f, 1f);
        AddChild(_root);
        BuildTotemRow(PlayerIds.Opponent(Viewer), 0);
        BuildSlotRow(PlayerIds.Opponent(Viewer), 1, OpponentSlotFill);
        BuildSlotRow(Viewer, 2, OwnSlotFill);
        BuildTotemRow(Viewer, 3);
        BuildInfo();
        BuildMarginCards();
    }

    // Where the hand (left) and the deck (right) will go: each side margin is
    // exactly one card wide (BOARD_DESIGN.md), level with the own card row.
    private void BuildMarginCards()
    {
        var rows = BoardLayoutSpec.RowFractions;
        var columns = BoardLayoutSpec.ColumnFractions;
        var hand = MakePanel(MarginFill, CellBorder, 0f, rows[2], columns[0], rows[3]);
        hand.AddChild(MakeCardView(BoardAssets.CardKey));
        hand.AddChild(MakeLabel("Hand", 22, CellBorder));
        _root.AddChild(hand);

        var deck = MakePanel(MarginFill, CellBorder, columns[^1], rows[2], 1f, rows[3]);
        deck.AddChild(MakeCardView(BoardAssets.CardKey));
        deck.AddChild(MakeLabel("Deck", 22, CellBorder));
        _root.AddChild(deck);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        if (key.Keycode == Key.R)
        {
            _seed++;
            NewMatch();
        }
        else if (key.Keycode == Key.M)
        {
            _mirrorMode = _mirrorMode == MirrorMode.ShuffledMirror
                ? MirrorMode.PerfectMirror
                : MirrorMode.ShuffledMirror;
            NewMatch();
        }
    }

    private void NewMatch()
    {
        var command = new SetupMatchCommand
        {
            Seed = _seed,
            MirrorMode = _mirrorMode,
            PlayerADeckDefinitionIds = System.Array.Empty<string>(),
            PlayerBDeckDefinitionIds = System.Array.Empty<string>(),
        };
        _world = MatchSetupSystem.Apply(command, _catalog).World;
        Refresh();
    }

    private void OnSlotInput(InputEvent @event, PlayerId player, int slot)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            return;
        }

        if (_world.Board.Side(player).IsOccupied(slot))
        {
            BoardPlacementSystem.Apply(_world, new ClearSlotCommand { Player = player, Slot = slot });
        }
        else
        {
            BoardPlacementSystem.Apply(_world, new PlaceBlankCardCommand { Player = player, Slot = slot });
        }

        Refresh();
    }

    private void Refresh()
    {
        var view = ProjectionSystem.Project(_world, Viewer);
        Render(Viewer, view.OwnBoard);
        Render(PlayerIds.Opponent(Viewer), view.OpponentBoard);

        string mode = _mirrorMode == MirrorMode.ShuffledMirror ? "Shuffled Mirror" : "Perfect Mirror";
        _info.Text =
            $"G01 board preview\n\nseed {_seed}\n{mode}\n\n" +
            "R: new seed\nM: switch mirror mode\nClick a slot: place or remove a blank card\n\n" +
            "top row: opponent\nbottom row: you" +
            (_assetError.Length > 0 ? $"\n\nART NOT LOADED:\n{_assetError}" : string.Empty);
    }

    private void Render(PlayerId player, BoardSideView board)
    {
        for (int slot = BoardGeometry.FirstSlot; slot <= BoardGeometry.LastSlot; slot++)
        {
            _cards[(player, slot)].Visible = board.UnitSlots[slot - 1].HasValue;
        }

        foreach (var placement in board.Totems)
        {
            _assets?.ShowIn(_totems[(player, placement.Position)], BoardAssets.KeyOf(placement.Type));
        }
    }

    private void BuildSlotRow(PlayerId player, int band, Color slotFill)
    {
        var rows = BoardLayoutSpec.RowFractions;
        var columns = BoardLayoutSpec.ColumnFractions;
        for (int slot = BoardGeometry.FirstSlot; slot <= BoardGeometry.LastSlot; slot++)
        {
            var cell = MakePanel(slotFill, CellBorder, columns[slot - 1], rows[band], columns[slot], rows[band + 1]);
            cell.MouseFilter = MouseFilterEnum.Stop;
            cell.MouseDefaultCursorShape = CursorShape.PointingHand;
            int capturedSlot = slot;
            cell.GuiInput += @event => OnSlotInput(@event, player, capturedSlot);
            cell.AddChild(MakeLabel(slot.ToString(), 28, DimText));
            _root.AddChild(cell);

            // The card fills its slot exactly (BOARD_DESIGN.md).
            var card = MakeCardView(BoardAssets.CardKey);
            card.Visible = false;
            cell.AddChild(card);
            _cards[(player, slot)] = card;
        }
    }

    private void BuildTotemRow(PlayerId player, int band)
    {
        var rows = BoardLayoutSpec.RowFractions;
        var columns = BoardLayoutSpec.ColumnFractions;
        foreach (var (left, right) in BoardLayoutSpec.TotemCellSpans)
        {
            var position = (TotemPosition)(left / 2);
            var cell = MakePanel(CellFill, CellBorder, columns[left], rows[band], columns[right], rows[band + 1]);
            _root.AddChild(cell);

            var letter = MakeLabel(position.ToString(), 20, DimText);
            letter.HorizontalAlignment = HorizontalAlignment.Left;
            letter.VerticalAlignment = VerticalAlignment.Top;
            letter.OffsetLeft = 10;
            letter.OffsetTop = 6;
            cell.AddChild(letter);

            // The totem stands bottom-centre in its cell at the board's scale.
            var totem = new AssetView { PixelsPerMeter = BoardPixelsPerMeter };
            Place(totem, 0f, 0f, 1f, 1f);
            cell.AddChild(totem);
            _totems[(player, position)] = totem;
        }
    }

    private void BuildInfo()
    {
        var columns = BoardLayoutSpec.ColumnFractions;
        var margin = MakeRect(Background, 0f, 0f, columns[0], 1f);
        _info = MakeLabel(string.Empty, 20, DimText);
        _info.HorizontalAlignment = HorizontalAlignment.Left;
        _info.VerticalAlignment = VerticalAlignment.Top;
        _info.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _info.OffsetLeft = 24;
        _info.OffsetTop = 24;
        _info.OffsetRight = -16;
        margin.AddChild(_info);
        _root.AddChild(margin);
    }

    private AssetView MakeCardView(string key)
    {
        var view = new AssetView { PixelsPerMeter = BoardPixelsPerMeter };
        Place(view, 0f, 0f, 1f, 1f);
        _assets?.ShowIn(view, key);
        return view;
    }

    // One card fills one column: the board's meters-to-pixels factor, shared
    // by every asset so totems and cards keep game04's proportions.
    private float BoardPixelsPerMeter()
    {
        if (_assets is null)
        {
            return 1f;
        }

        var columns = BoardLayoutSpec.ColumnFractions;
        return GetViewportRect().Size.X * (columns[1] - columns[0]) / _assets.CardWidth;
    }

    private static ColorRect MakeRect(Color colour, float left, float top, float right, float bottom)
    {
        var rect = new ColorRect { Color = colour, MouseFilter = MouseFilterEnum.Ignore };
        Place(rect, left, top, right, bottom);
        return rect;
    }

    private static Panel MakePanel(Color fill, Color border, float left, float top, float right, float bottom)
    {
        var panel = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", MakeStyle(fill, border, 2));
        Place(panel, left, top, right, bottom);
        return panel;
    }

    private static StyleBoxFlat MakeStyle(Color fill, Color border, int borderWidth)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = border };
        style.SetBorderWidthAll(borderWidth);
        return style;
    }

    private static Label MakeLabel(string text, int fontSize, Color colour)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", colour);
        Place(label, 0f, 0f, 1f, 1f);
        return label;
    }

    private static void Place(Control control, float left, float top, float right, float bottom)
    {
        control.AnchorLeft = left;
        control.AnchorTop = top;
        control.AnchorRight = right;
        control.AnchorBottom = bottom;
        control.OffsetLeft = 0;
        control.OffsetTop = 0;
        control.OffsetRight = 0;
        control.OffsetBottom = 0;
    }
}
