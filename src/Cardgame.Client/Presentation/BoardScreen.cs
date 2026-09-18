using System.Collections.Generic;
using System.Linq;
using Cardgame.Assets;
using Cardgame.Core;
using Cardgame.Core.Board;
using Cardgame.Core.Combat;
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
/// totem's segments show the mana left, the life totem's the health left, and
/// the time totem drains through its segments over the 34 seconds the turn
/// has. Clicking the own time totem or pressing Tab ends the turn.
///
/// Attacking (G05, G06): click an own unit that may attack; the fields and
/// unprotected totems it can aim at get a red attack frame, hovering one shows
/// what the attack would hit, clicking it attacks. A pattern without a target
/// frames every field it hits.
/// Coins are a strip at the top of the right margin.
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
    private static readonly Color ManaDebtColour = new("D64545");

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
    private int? _attackerSlot;
    private FieldRef? _hovered;
    private TotemRef? _hoveredTotem;
    private readonly Dictionary<(int Row, int Slot), AttackFrame> _frames = new();
    private readonly Dictionary<(int Row, TotemPosition Position), AttackFrame> _totemFrames = new();
    private readonly List<AssetView> _coins = new();
    private const int MaxCoinsShown = 20;

    private Control _root = null!;
    private CardControl _deck = null!;
    private Label _debugText = null!;
    private Label _outcomeText = null!;
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
        BuildOutcomeText();
        BuildCoins();

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

        if (_world.Outcome is not null)
        {
            return;
        }

        _elapsed += delta;
        if (_elapsed >= TurnSeconds)
        {
            // A Totem of Time beaten to 0 gives its owner a turn that is over
            // as it begins — they still draw and still refill (§8.6).
            EndTurn(timedOut: true);
            return;
        }

        UpdateTimeTotem();
    }

    private void StartClock() => _elapsed = 0;

    /// <summary>The seconds the active player's Totem of Time grants this turn.</summary>
    private int TurnSeconds => _world.Zones(_world.Turn.ActivePlayer).Time.Seconds;

    private void EndTurn(bool timedOut)
    {
        var result = TurnSystem.Apply(_world, new EndTurnCommand { Player = _world.Turn.ActivePlayer, TimedOut = timedOut });
        if (result is TurnStartedEvent or MatchEndedEvent)
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

        int seconds = TurnSeconds;
        double left = seconds <= 0 ? 0 : 1 - _elapsed / seconds;
        float cutoff = _segmentBottom + (float)System.Math.Clamp(left, 0, 1) * (_segmentTop - _segmentBottom);
        ((ShaderMaterial)_totemMasks[key].Material).SetShaderParameter("cutoff", cutoff);
    }

    private void ClearSelection()
    {
        _selectedCard = null;
        _selectedSlot = null;
        _attackerSlot = null;
        _hovered = null;
        _hoveredTotem = null;
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
        _attackerSlot = null;
        Refresh();
    }

    private void OnTotemClicked(int row, TotemPosition position)
    {
        if (row == OwnRow && !_picker.Visible
            && _view.OwnBoard.Totems.Any(t => t.Position == position && t.Type == TotemType.Time))
        {
            EndTurn(timedOut: false);
            return;
        }

        var totem = new TotemRef(row == OwnRow ? _viewer : PlayerIds.Opponent(_viewer), position);
        if (_attackerSlot is int attacker && AttackTotems().Contains(totem))
        {
            AttackTotem(attacker, totem);
        }
    }

    private void OnTotemHovered(int row, TotemPosition position, bool entered)
    {
        _hoveredTotem = entered
            ? new TotemRef(row == OwnRow ? _viewer : PlayerIds.Opponent(_viewer), position)
            : null;
        RenderAttackFrames();
    }

    private FieldRef FieldOf(int row, int slot) => new(row == OwnRow ? _viewer : PlayerIds.Opponent(_viewer), slot);

    private int RowOf(PlayerId side) => side == _viewer ? OwnRow : OpponentRow;

    private void OnSlotClicked(int row, int slot)
    {
        var field = FieldOf(row, slot);
        if (_attackerSlot is int attacker)
        {
            if (row == OwnRow && slot == attacker)
            {
                ClearSelection();
                Refresh();
                return;
            }

            if (AttackFields().Contains(field))
            {
                Attack(attacker, field);
                return;
            }
        }

        if (row == OwnRow && _view.OwnBoard.UnitSlots[slot - 1] is { } unit)
        {
            if (_view.Cards[unit].CanAttackNow)
            {
                ClearSelection();
                _attackerSlot = slot;
            }
            else
            {
                _lastMessage = CombatRules.WhyCannotAttack(_world, _assets!.Catalog, _viewer, slot) ?? string.Empty;
            }

            Refresh();
            return;
        }

        if (row != OwnRow || _selectedCard is not { } card)
        {
            return;
        }

        _selectedSlot = slot;
        Refresh();
        _picker.Open(_assets!, _view.Cards[card].DefinitionId, _view.OwnMana, _debug);
    }

    // Fields the selected attacker frames: its targets, or — for an attack
    // without a target — everything it would hit.
    private IReadOnlyList<FieldRef> AttackFields()
    {
        if (_attackerSlot is not int slot)
        {
            return System.Array.Empty<FieldRef>();
        }

        var profile = CombatRules.ProfileOf(_world, _assets!.Catalog, _viewer, slot);
        return profile is { NeedsTarget: false }
            ? CombatRules.AffectedFields(_world, _assets.Catalog, _viewer, slot, null).Select(f => f.Field).ToArray()
            : CombatRules.Targets(_world, _assets.Catalog, _viewer, slot);
    }

    // The opposing totems the selected attacker may aim at (§4, §8.1.1).
    private IReadOnlyList<TotemRef> AttackTotems() =>
        _attackerSlot is int slot
            ? CombatRules.TotemTargets(_world, _assets!.Catalog, _viewer, slot)
            : System.Array.Empty<TotemRef>();

    private void AttackTotem(int attackerSlot, TotemRef totem)
    {
        var before = _view;
        var result = CombatSystem.Apply(_world, _assets!.Catalog, new AttackCommand
        {
            Player = _viewer,
            AttackerSlot = attackerSlot,
            TotemTarget = totem,
        });

        if (result is UnitAttackedEvent attacked)
        {
            _attackerSlot = null;
            _hovered = null;
            _hoveredTotem = null;
            if (attacked.TurnEnded is not null)
            {
                _bank = 0;
                StartClock();
            }

            PlayAttack(before, attacked);
        }

        Apply(result);
    }

    private void Attack(int attackerSlot, FieldRef target)
    {
        var profile = CombatRules.ProfileOf(_world, _assets!.Catalog, _viewer, attackerSlot);
        var before = _view;
        var result = CombatSystem.Apply(_world, _assets.Catalog, new AttackCommand
        {
            Player = _viewer,
            AttackerSlot = attackerSlot,
            Target = profile is { NeedsTarget: true } ? target : null,
        });

        if (result is UnitAttackedEvent attacked)
        {
            _attackerSlot = null;
            _hovered = null;
            PlayAttack(before, attacked);
        }

        Apply(result);
    }

    // Feedback: the attacker lunges towards its target, hit fields flash red,
    // destroyed cards fade out where they stood.
    private void PlayAttack(PlayerView before, UnitAttackedEvent attacked)
    {
        var attackerCell = _slotCells[(OwnRow, attacked.AttackerSlot)];
        var card = _slotCards[(OwnRow, attacked.AttackerSlot)];
        Control aim = attacked.Target is { } target ? _slotCells[(RowOf(target.Side), target.Slot)]
            : attacked.TotemTarget is { } totem ? _totemCells[(RowOf(totem.Side), totem.Position)]
            : attackerCell;
        var lunge = (aim.GlobalPosition - attackerCell.GlobalPosition).LimitLength(1f) * 40f;
        var tween = CreateTween();
        tween.TweenProperty(card, "position", lunge, 0.08f);
        tween.TweenProperty(card, "position", Vector2.Zero, 0.14f);

        foreach (var hit in attacked.Hits)
        {
            var cell = _slotCells[(RowOf(hit.Field.Side), hit.Field.Slot)];
            var flash = CreateTween();
            flash.TweenProperty(cell, "modulate", new Color(1f, 0.45f, 0.45f), 0.08f);
            flash.TweenProperty(cell, "modulate", Colors.White, 0.35f);

            if (hit.Destroyed && before.Cards.TryGetValue(hit.Card, out var gone))
            {
                var ghost = new CardControl(BoardPixelsPerMeter) { MouseFilter = MouseFilterEnum.Ignore };
                CardControl.Fill(ghost);
                cell.AddChild(ghost);
                ghost.ShowCard(_assets!, gone.DefinitionId, Tier(gone.DefinitionId, gone.Tier ?? 1), false, false, gone.Damage + hit.Damage);
                var fade = CreateTween();
                fade.TweenProperty(ghost, "modulate", new Color(1f, 0.3f, 0.3f, 0f), 0.6f);
                fade.TweenCallback(Callable.From(ghost.QueueFree));
            }
        }

        foreach (var hit in attacked.TotemHits)
        {
            var cell = _totemCells[(RowOf(hit.Totem.Side), hit.Totem.Position)];
            var flash = CreateTween();
            flash.TweenProperty(cell, "modulate", new Color(1f, 0.45f, 0.45f), 0.08f);
            flash.TweenProperty(cell, "modulate", Colors.White, 0.35f);
        }
    }

    private void OnSlotHovered(int row, int slot, bool entered)
    {
        _hovered = entered ? FieldOf(row, slot) : null;
        RenderAttackFrames();
    }

    private void RenderAttackFrames()
    {
        foreach (var frame in _frames.Values)
        {
            frame.Visible = false;
            frame.Affected = false;
        }

        foreach (var frame in _totemFrames.Values)
        {
            frame.Visible = false;
            frame.Affected = false;
        }

        var fields = AttackFields();
        foreach (var field in fields)
        {
            _frames[(RowOf(field.Side), field.Slot)].Visible = true;
        }

        var totems = AttackTotems();
        foreach (var totem in totems)
        {
            _totemFrames[(RowOf(totem.Side), totem.Position)].Visible = true;
        }

        if (_attackerSlot is not int slot)
        {
            return;
        }

        if (_hoveredTotem is { } hoveredTotem && totems.Contains(hoveredTotem))
        {
            foreach (var (totem, _, _) in CombatRules.AffectedTotems(_world, _assets!.Catalog, _viewer, slot, hoveredTotem))
            {
                var frame = _totemFrames[(RowOf(totem.Side), totem.Position)];
                frame.Visible = true;
                frame.Affected = true;
            }

            return;
        }

        if (_hovered is not { } hovered || !fields.Contains(hovered))
        {
            return;
        }

        var profile = CombatRules.ProfileOf(_world, _assets!.Catalog, _viewer, slot);
        var aim = profile is { NeedsTarget: true } ? hovered : (FieldRef?)null;
        foreach (var (field, _) in CombatRules.AffectedFields(_world, _assets.Catalog, _viewer, slot, aim))
        {
            var frame = _frames[(RowOf(field.Side), field.Slot)];
            frame.Visible = true;
            frame.Affected = true;
        }
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

        RenderSide(OwnRow, _view.OwnBoard, _view.OwnMana, _view.OwnManaDebt, _view.OwnLife, _view.OwnSeconds);
        RenderSide(
            OpponentRow, _view.OpponentBoard, _view.OpponentMana, _view.OpponentManaDebt,
            _view.OpponentLife, _view.OpponentSeconds);

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

        RenderAttackFrames();
        RenderCoins();
        _outcomeText.Visible = _view.Outcome is not null;
        if (_view.Outcome is { } outcome)
        {
            _outcomeText.Text = outcome.IsDraw
                ? $"Draw — {outcome.Reason}\nR: new match"
                : $"{outcome.Winner} wins — {outcome.Reason}\nR: new match";
        }

        _debugText.Visible = _debug;
        _debugText.Text =
            $"{_viewer} · round {_view.Round} · seed {_seed} · {_matchMode}\n" +
            $"clock {_elapsed:0.0}s of {_view.OwnSeconds}{(_paused ? " · PAUSED" : string.Empty)} · " +
            $"quiet rounds {_view.Round - _world.Turn.LastLifeDamageRound}/{TurnSystem.DrawAfterQuietRounds}\n" +
            $"own: life {_view.OwnLife}/{_view.MaxLife}, mana {_view.OwnMana}/{_view.OwnMaxMana} (owed {_view.OwnManaDebt}), " +
            $"time {_view.OwnSeconds}s, hand {_view.OwnHandCount}, deck {_view.OwnDeckCount}, bank {_bank + 1}/2, coins {_view.OwnCoins}\n" +
            $"opponent: life {_view.OpponentLife}/{_view.MaxLife}, mana {_view.OpponentMana}/{_view.OpponentMaxMana} (owed {_view.OpponentManaDebt}), " +
            $"time {_view.OpponentSeconds}s, hand {_view.OpponentHandCount}, deck {_view.OpponentDeckCount}, coins {_view.OpponentCoins}\n\n" +
            "click own unit → framed unit or totem: attack\n" +
            "click hand card → free slot → 1/2/3\n" +
            "Tab or click own time totem: end turn\n" +
            "Q bank · P pause · F refill mana\nR new match · M mode · D overlay · Esc cancel\n\n" +
            _lastMessage;
    }

    private void RenderSide(int row, BoardSideView board, int mana, int manaDebt, int life, int seconds)
    {
        for (int slot = BoardGeometry.FirstSlot; slot <= BoardGeometry.LastSlot; slot++)
        {
            var control = _slotCards[(row, slot)];
            if (board.UnitSlots[slot - 1] is { } id && _view.Cards.TryGetValue(id, out var card))
            {
                bool attacking = row == OwnRow && _attackerSlot == slot;
                control.ShowCard(_assets!, card.DefinitionId, Tier(card.DefinitionId, card.Tier ?? 1), _debug, attacking, card.Damage);
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
            _totems[key].Display(geometry, part =>
            {
                if (part.Kind == AssetPartKind.Stroke)
                {
                    return stroke;
                }

                if (SegmentNumber(part.ComponentName) is not int n)
                {
                    return fill;
                }

                // Mana counts up from segment01 and is taken off the top;
                // mana that is owed turns the lowest segments red (§5.2).
                // A Totem of Life's lost segments fade to a faint red (§5.1).
                return placement.Type switch
                {
                    TotemType.Mana => n <= manaDebt ? ManaDebtColour : n <= mana ? lit : fill,
                    TotemType.Life => n <= life ? lit : fill.Lerp(stroke, 0.18f),
                    _ => fill,
                };
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
            mask.Display(segments, _ => lit);
            if (running)
            {
                _activeTimeTotem = key;
                _segmentBottom = segments.MinY;
                _segmentTop = segments.MaxY;
                UpdateTimeTotem();
            }
            else
            {
                // The waiting player's totem shows the seconds their next turn
                // still has: what an attack on it took is missing from the top.
                float left = _view.MaxSeconds > 0 ? seconds / (float)_view.MaxSeconds : 0f;
                ((ShaderMaterial)mask.Material).SetShaderParameter(
                    "cutoff", segments.MinY + System.Math.Clamp(left, 0f, 1f) * (segments.MaxY - segments.MinY));
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
            cell.MouseEntered += () => OnSlotHovered(row, capturedSlot, true);
            cell.MouseExited += () => OnSlotHovered(row, capturedSlot, false);
            cell.AddChild(MakeLabel(slot.ToString(), 28, DimText));
            _root.AddChild(cell);
            _slotCells[(row, slot)] = cell;

            var card = new CardControl(BoardPixelsPerMeter) { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            CardControl.Fill(card);
            cell.AddChild(card);
            _slotCards[(row, slot)] = card;

            var frame = new AttackFrame();
            CardControl.Fill(frame);
            cell.AddChild(frame);
            _frames[(row, slot)] = frame;
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

            var frame = new AttackFrame();
            CardControl.Fill(frame);
            cell.AddChild(frame);
            _totemFrames[(row, position)] = frame;

            cell.MouseFilter = MouseFilterEnum.Stop;
            cell.MouseEntered += () => OnTotemHovered(row, position, true);
            cell.MouseExited += () => OnTotemHovered(row, position, false);
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

    // The own coins: a strip across the top of the right margin, one coin
    // high. A coin is a quarter of a totem's height across; up to 20 overlap
    // from left to right.
    private void BuildCoins()
    {
        for (int i = 0; i < MaxCoinsShown; i++)
        {
            var coin = new AssetView { PixelsPerMeter = CoinPixelsPerMeter, Visible = false };
            _root.AddChild(coin);
            _coins.Add(coin);
        }
    }

    private float CoinDiameter() =>
        GetViewportRect().Size.Y * (BoardLayoutSpec.RowFractions[1] - BoardLayoutSpec.RowFractions[0]) * _assets!.CellFill / 4f;

    private float CoinPixelsPerMeter()
    {
        var coin = _assets!.Coin;
        return CoinDiameter() / System.Math.Max(coin.Width, coin.Height);
    }

    private void RenderCoins()
    {
        var viewport = GetViewportRect().Size;
        float diameter = CoinDiameter();
        float left = viewport.X * BoardLayoutSpec.ColumnFractions[^1];
        float width = viewport.X - left;
        float step = (width - diameter) / (MaxCoinsShown - 1);
        var (geometry, fill, stroke) = _assets!.CoinArt;
        int shown = System.Math.Min(_view.OwnCoins, MaxCoinsShown);
        for (int i = 0; i < MaxCoinsShown; i++)
        {
            var view = _coins[i];
            view.Visible = i < shown;
            if (!view.Visible)
            {
                continue;
            }

            // The coin's pivot is its centre; AssetView puts the pivot at the
            // bottom centre, so the control is placed half a coin lower.
            view.Position = new Vector2(left + i * step, -diameter / 2f);
            view.Size = new Vector2(diameter, diameter);
            view.Display(geometry, part => part.Kind == AssetPartKind.Fill ? fill : stroke);
        }
    }

    // The match's end, shown over the board; R starts the next one.
    private void BuildOutcomeText()
    {
        _outcomeText = MakeLabel(string.Empty, 54, new Color("F5F2E8"));
        Place(_outcomeText, 0f, 0.42f, 1f, 0.58f);
        _outcomeText.Visible = false;
        _root.AddChild(_outcomeText);
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
