using System.Collections.Generic;
using System.Linq;
using Cardgame.Assets;
using Cardgame.Core;
using Cardgame.Core.Board;
using Cardgame.Core.Commands;
using Cardgame.Core.Events;
using Cardgame.Core.Model;
using Cardgame.Core.Snapshot;
using Cardgame.Core.Systems;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// The match screen (G01 board, G02 cards, G04 turns). It draws a
/// <see cref="PlayerView"/> and sends commands; it decides nothing itself.
/// Hot-seat: the screen always belongs to the active player.
///
/// The own hand sits in the left margin as two banks of four cards, one shown
/// at a time (Q). Playing a unit: click a hand card, click a free own slot,
/// then pick the tier with 1, 2 or 3 (<see cref="TierPicker"/>). The mana
/// totem's segments show the mana left; the time totem drains through its
/// segments, base time first, then — after a flip — bonus time. Clicking the
/// own time totem or pressing Tab ends the turn.
///
/// Dev bootstrap: there is no server yet, so this node holds the world,
/// keeps the turn clock (CORE-01 gives real time to the server later) and
/// applies commands directly. P pauses the clock, F refills your mana, R
/// starts a new match, M cycles the match mode, D shows the debug overlay.
///
/// Both rows count 1-6 from the left and the totem places run A, B, C from
/// the left on both sides: the view is mirrored, not turned (BoardGeometry).
/// </summary>
public partial class BoardScreen : Control
{
    private const int BankSize = 4;
    private const int OwnRow = 0;
    private const int OpponentRow = 1;

    private static readonly Color Background = new(0.055f, 0.09f, 0.07f);
    private static readonly Color CellFill = new(0.10f, 0.14f, 0.12f);
    private static readonly Color CellBorder = new(0.30f, 0.36f, 0.33f);
    private static readonly Color OwnSlotFill = new(0.13f, 0.19f, 0.25f);
    private static readonly Color OpponentSlotFill = new(0.22f, 0.14f, 0.17f);
    private static readonly Color SelectedSlotBorder = new("E3B341");
    private static readonly Color DimText = new(0.55f, 0.60f, 0.57f);

    private readonly Dictionary<(int Row, int Slot), CardControl> _slotCards = new();
    private readonly Dictionary<(int Row, int Slot), Panel> _slotCells = new();
    private readonly Dictionary<(int Row, TotemPosition Position), AssetView> _totems = new();
    private readonly CardControl[] _handCards = new CardControl[BankSize];

    private BoardAssets? _assets;
    private WorldState _world = null!;
    private PlayerView _view = null!;
    private PlayerId _viewer = PlayerId.PlayerA;
    private double _elapsed;
    private bool _paused;
    private bool _inBonus;
    private readonly Dictionary<(int Row, TotemPosition Position), AssetView> _totemMasks = new();
    private readonly Dictionary<(int Row, TotemPosition Position), Control> _totemCells = new();
    private (int Row, TotemPosition Position)? _activeTimeTotem;
    private float _segmentBottom;
    private float _segmentTop;
    private ulong _seed = 1;
    private MatchMode _matchMode = MatchMode.ShuffledMirror;
    private static readonly Shader DrainShader = GD.Load<Shader>("res://Presentation/DrainMask.gdshader");
    private int _bank;
    private bool _debug;
    private CardInstanceId? _selectedCard;
    private int? _selectedSlot;
    private string _lastMessage = string.Empty;

    private Control _root = null!;
    private CardControl _deck = null!;
    private Label _debugText = null!;
    private Label _error = null!;
    private TierPicker _picker = null!;

    public override void _Ready()
    {
        _root = MakeRect(Background, 0f, 0f, 1f, 1f);
        AddChild(_root);

        _error = MakeLabel(string.Empty, 20, new Color("EF8354"));
        _error.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _root.AddChild(_error);

        try
        {
            _assets = BoardAssets.Load();
        }
        catch (System.Exception exception)
        {
            _error.Text = $"Board data not loaded:\n{exception.Message}";
            GD.PushError($"board data not loaded: {exception.Message}");
            return;
        }

        BuildTotemRow(OpponentRow, 0);
        BuildSlotRow(OpponentRow, 1, OpponentSlotFill);
        BuildSlotRow(OwnRow, 2, OwnSlotFill);
        BuildTotemRow(OwnRow, 3);
        BuildHand();
        BuildDeck();
        BuildDebugText();

        _picker = new TierPicker();
        _picker.TierChosen += PlaySelected;
        _picker.Cancelled += () =>
        {
            _selectedSlot = null;
            Refresh();
        };
        AddChild(_picker);

        NewMatch();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_assets is null || _picker.Visible || @event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        switch (key.Keycode)
        {
            case Key.R:
                _seed++;
                NewMatch();
                break;
            case Key.M:
                _matchMode = _matchMode switch
                {
                    MatchMode.ShuffledMirror => MatchMode.PerfectMirror,
                    MatchMode.PerfectMirror => MatchMode.Constructed,
                    _ => MatchMode.ShuffledMirror,
                };
                NewMatch();
                break;
            case Key.P:
                _paused = !_paused;
                Refresh();
                break;
            case Key.Q:
                _bank = 1 - _bank;
                Refresh();
                break;
            case Key.D:
                _debug = !_debug;
                Refresh();
                break;
            case Key.F:
                Apply(CardPlaySystem.Apply(_world, new RefillManaCommand { Player = _viewer }));
                break;
            case Key.Tab:
                EndTurn(timedOut: false);
                break;
            case Key.Escape:
                ClearSelection();
                Refresh();
                break;
            default:
                return;
        }

        GetViewport().SetInputAsHandled();
    }

    private void NewMatch()
    {
        var command = new SetupMatchCommand
        {
            Seed = _seed,
            MatchMode = _matchMode,
            PlayerADeckDefinitionIds = _assets!.StarterDeck,
            PlayerBDeckDefinitionIds = _assets.StarterDeck,
        };
        _world = MatchSetupSystem.Apply(command, _assets.Catalog).World;
        _bank = 0;
        StartClock();
        _lastMessage = string.Empty;
        ClearSelection();
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (_assets is null || _world is null || _paused)
        {
            UpdateTimeTotem();
            return;
        }

        _elapsed += delta;
        var clock = _world.Clock;
        if (!_inBonus && _elapsed >= clock.BaseSeconds)
        {
            _inBonus = true;
            FlipActiveTimeTotem();
            Refresh();
        }

        if (_elapsed >= clock.TotalSeconds)
        {
            EndTurn(timedOut: true);
            return;
        }

        UpdateTimeTotem();
    }

    private void StartClock()
    {
        _elapsed = 0;
        _inBonus = false;
    }

    private void EndTurn(bool timedOut)
    {
        var result = TurnSystem.Apply(_world, new EndTurnCommand { Player = _world.Turn.ActivePlayer, TimedOut = timedOut });
        if (result is TurnStartedEvent)
        {
            ClearSelection();
            _bank = 0;
            StartClock();
        }

        Apply(result);
    }

    // The active time totem's lit segments end at the height of the time
    // left in the current phase, draining from the top.
    private void UpdateTimeTotem()
    {
        if (_activeTimeTotem is not { } key || _world is null)
        {
            return;
        }

        var clock = _world.Clock;
        double left = _inBonus
            ? 1 - (_elapsed - clock.BaseSeconds) / clock.BonusSeconds
            : 1 - _elapsed / clock.BaseSeconds;
        float cutoff = _segmentBottom + (float)System.Math.Clamp(left, 0, 1) * (_segmentTop - _segmentBottom);
        ((ShaderMaterial)_totemMasks[key].Material).SetShaderParameter("cutoff", cutoff);
    }

    private void FlipActiveTimeTotem()
    {
        if (_activeTimeTotem is not { } key)
        {
            return;
        }

        var cell = _totemCells[key];
        cell.PivotOffset = cell.Size / 2;
        var tween = CreateTween();
        tween.TweenProperty(cell, "scale", new Vector2(1f, 0f), 0.18f);
        tween.TweenProperty(cell, "scale", Vector2.One, 0.18f);
    }

    private void ClearSelection()
    {
        _selectedCard = null;
        _selectedSlot = null;
        _picker?.Close();
    }

    private void OnHandCardClicked(int index)
    {
        int handIndex = _bank * BankSize + index;
        if (handIndex >= _view.OwnHandCards.Count)
        {
            return;
        }

        var card = _view.OwnHandCards[handIndex];
        _selectedCard = _selectedCard == card ? null : card;
        _selectedSlot = null;
        Refresh();
    }

    private void OnTotemClicked(int row, TotemPosition position)
    {
        if (row == OwnRow && !_picker.Visible
            && _view.OwnBoard.Totems.Any(t => t.Position == position && t.Type == TotemType.Time))
        {
            EndTurn(timedOut: false);
        }
    }

    private void OnSlotClicked(int row, int slot)
    {
        if (row != OwnRow || _selectedCard is not { } card || _view.OwnBoard.UnitSlots[slot - 1].HasValue)
        {
            return;
        }

        _selectedSlot = slot;
        Refresh();
        _picker.Open(_assets!, _view.Cards[card].DefinitionId, _view.OwnMana, _debug);
    }

    private void PlaySelected(int tier)
    {
        if (_selectedCard is not { } card || _selectedSlot is not { } slot)
        {
            return;
        }

        var result = CardPlaySystem.Apply(_world, _assets!.Catalog, new PlayUnitCommand
        {
            Player = _viewer,
            Card = card,
            Tier = tier,
            Slot = slot,
        });
        if (result is UnitPlayedEvent)
        {
            ClearSelection();
        }

        Apply(result);
    }

    private void Apply(IEvent result)
    {
        _lastMessage = result is CommandRejectedEvent rejected ? $"rejected: {rejected.Reason}" : result.GetType().Name;
        if (result is CommandRejectedEvent)
        {
            GD.Print(_lastMessage);
        }

        Refresh();
    }

    private void Refresh()
    {
        _viewer = _world.Turn.ActivePlayer;
        _view = ProjectionSystem.Project(_world, _viewer);
        _activeTimeTotem = null;
        if (_bank * BankSize >= System.Math.Max(_view.OwnHandCount, 1))
        {
            _bank = 0;
        }

        RenderSide(OwnRow, _view.OwnBoard, _view.OwnMana);
        RenderSide(OpponentRow, _view.OpponentBoard, _view.OpponentMana);

        for (int i = 0; i < BankSize; i++)
        {
            int handIndex = _bank * BankSize + i;
            var control = _handCards[i];
            if (handIndex >= _view.OwnHandCount)
            {
                control.Visible = false;
                continue;
            }

            var id = _view.OwnHandCards[handIndex];
            var card = _view.Cards[id];
            control.ShowCard(_assets!, card.DefinitionId, Tier(card.DefinitionId, 1), _debug, id == _selectedCard);
        }

        if (_view.OwnDeckCount > 0)
        {
            _deck.ShowBack(_assets!);
        }
        else
        {
            _deck.Visible = false;
        }

        _debugText.Visible = _debug;
        _debugText.Text =
            $"{_viewer} · round {_view.Round} · seed {_seed} · {_matchMode}\n" +
            $"clock {(_inBonus ? "bonus" : "base")} {_elapsed:0.0}s of {_view.BaseSeconds}+{_view.BonusSeconds}{(_paused ? " · PAUSED" : string.Empty)}\n" +
            $"own: mana {_view.OwnMana}/{_view.OwnMaxMana}, hand {_view.OwnHandCount}, deck {_view.OwnDeckCount}, bank {_bank + 1}/2\n" +
            $"opponent: mana {_view.OpponentMana}/{_view.OpponentMaxMana}, hand {_view.OpponentHandCount}, deck {_view.OpponentDeckCount}\n\n" +
            "click hand card → free slot → 1/2/3\n" +
            "Tab or click own time totem: end turn\n" +
            "Q bank · P pause · F refill mana\nR new match · M mode · D overlay · Esc cancel\n\n" +
            _lastMessage;
    }

    private void RenderSide(int row, BoardSideView board, int mana)
    {
        for (int slot = BoardGeometry.FirstSlot; slot <= BoardGeometry.LastSlot; slot++)
        {
            var control = _slotCards[(row, slot)];
            if (board.UnitSlots[slot - 1] is { } id && _view.Cards.TryGetValue(id, out var card))
            {
                control.ShowCard(_assets!, card.DefinitionId, Tier(card.DefinitionId, card.Tier ?? 1), _debug, false);
            }
            else
            {
                control.Visible = false;
            }

            bool selected = row == OwnRow && _selectedSlot == slot;
            _slotCells[(row, slot)].AddThemeStyleboxOverride(
                "panel",
                MakeStyle(row == OwnRow ? OwnSlotFill : OpponentSlotFill, selected ? SelectedSlotBorder : CellBorder, selected ? 4 : 2));
        }

        foreach (var placement in board.Totems)
        {
            var (geometry, fill, stroke) = _assets!.Totem(placement.Type);
            var lit = stroke.Lightened(0.3f);
            var key = (row, placement.Position);
            bool isMana = placement.Type == TotemType.Mana;
            _totems[key].Display(geometry, part =>
            {
                if (part.Kind == AssetPartKind.Stroke)
                {
                    return stroke;
                }

                return isMana && SegmentNumber(part.ComponentName) is int n && n <= mana ? lit : fill;
            });

            var mask = _totemMasks[key];
            if (placement.Type != TotemType.Time)
            {
                mask.Clear();
                continue;
            }

            // The segments are the mask the remaining time shows through.
            var segments = geometry.Where(part => part.Kind == AssetPartKind.Fill && SegmentNumber(part.ComponentName) is not null);
            bool running = row == OwnRow;
            var colour = running && _inBonus ? stroke.Darkened(0.12f) : lit;
            mask.Display(segments, _ => colour);
            if (running)
            {
                _activeTimeTotem = key;
                _segmentBottom = segments.MinY;
                _segmentTop = segments.MaxY;
                UpdateTimeTotem();
            }
            else
            {
                ((ShaderMaterial)mask.Material).SetShaderParameter("cutoff", 1000f);
            }
        }
    }

    // "segment03" → 3; anything else → null.
    private static int? SegmentNumber(string name) =>
        name.StartsWith("segment", System.StringComparison.Ordinal) && int.TryParse(name.Substring(7), out int n) ? n : null;

    private CardTier Tier(string definitionId, int tier) => _assets!.Catalog.CardsById[definitionId].Tiers[tier - 1];

    private void BuildSlotRow(int row, int band, Color slotFill)
    {
        var rows = BoardLayoutSpec.RowFractions;
        var columns = BoardLayoutSpec.ColumnFractions;
        for (int slot = BoardGeometry.FirstSlot; slot <= BoardGeometry.LastSlot; slot++)
        {
            var cell = MakePanel(slotFill, CellBorder, columns[slot - 1], rows[band], columns[slot], rows[band + 1]);
            cell.MouseFilter = MouseFilterEnum.Stop;
            int capturedSlot = slot;
            cell.GuiInput += @event =>
            {
                if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    OnSlotClicked(row, capturedSlot);
                }
            };
            cell.AddChild(MakeLabel(slot.ToString(), 28, DimText));
            _root.AddChild(cell);
            _slotCells[(row, slot)] = cell;

            var card = new CardControl(BoardPixelsPerMeter) { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            CardControl.Fill(card);
            cell.AddChild(card);
            _slotCards[(row, slot)] = card;
        }
    }

    private void BuildTotemRow(int row, int band)
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

            var totem = new AssetView { PixelsPerMeter = BoardPixelsPerMeter, Fill = _assets!.CellFill };
            CardControl.Fill(totem);
            cell.AddChild(totem);
            _totems[(row, position)] = totem;

            var mask = new AssetView
            {
                PixelsPerMeter = BoardPixelsPerMeter,
                Fill = _assets.CellFill,
                Material = new ShaderMaterial { Shader = DrainShader },
            };
            CardControl.Fill(mask);
            cell.AddChild(mask);
            _totemMasks[(row, position)] = mask;
            _totemCells[(row, position)] = cell;

            cell.MouseFilter = MouseFilterEnum.Stop;
            cell.GuiInput += @event =>
            {
                if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    OnTotemClicked(row, position);
                }
            };
        }
    }

    // The own hand: the left margin split into four equal cells.
    private void BuildHand()
    {
        float right = BoardLayoutSpec.ColumnFractions[0];
        for (int i = 0; i < BankSize; i++)
        {
            var card = new CardControl { Visible = false };
            Place(card, 0f, i / (float)BankSize, right, (i + 1) / (float)BankSize);
            int index = i;
            card.Clicked += () => OnHandCardClicked(index);
            _root.AddChild(card);
            _handCards[i] = card;
        }
    }

    // The own deck: one card back in the right margin, level with the own card row.
    private void BuildDeck()
    {
        var rows = BoardLayoutSpec.RowFractions;
        _deck = new CardControl(BoardPixelsPerMeter) { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        Place(_deck, BoardLayoutSpec.ColumnFractions[^1], rows[2], 1f, rows[3]);
        _root.AddChild(_deck);
    }

    private void BuildDebugText()
    {
        var rows = BoardLayoutSpec.RowFractions;
        _debugText = MakeLabel(string.Empty, 15, Colors.White);
        _debugText.HorizontalAlignment = HorizontalAlignment.Left;
        _debugText.VerticalAlignment = VerticalAlignment.Top;
        _debugText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        Place(_debugText, BoardLayoutSpec.ColumnFractions[^1], 0f, 1f, rows[2]);
        _debugText.OffsetLeft = 12;
        _debugText.OffsetTop = 12;
        _debugText.OffsetRight = -8;
        _debugText.Visible = false;
        _root.AddChild(_debugText);
    }

    // One card fills one column: the board's meters-to-pixels factor, shared
    // by every asset on the board so totems and cards keep game04's proportions.
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
