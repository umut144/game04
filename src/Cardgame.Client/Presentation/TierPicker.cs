using System;
using Cardgame.Core.Model;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// The three tiers of a card side by side (GAME_DESIGN.md §7.3), shown after
/// a hand card and a free slot were chosen. 1, 2, 3 or a click picks a tier;
/// tiers the player cannot pay are dimmed and do nothing. Esc or a right
/// click cancels.
/// </summary>
public partial class TierPicker : Control
{
    private const float CardWidth = 0.2f;
    private const float CardHeight = 0.4572f;
    private const float Gap = 0.025f;

    private readonly CardControl[] _cards = new CardControl[3];
    private readonly Label[] _labels = new Label[3];
    private readonly bool[] _affordable = new bool[3];

    public event Action<int>? TierChosen;
    public event Action? Cancelled;

    public TierPicker()
    {
        MouseFilter = MouseFilterEnum.Stop;
        CardControl.Fill(this);
        Visible = false;

        var backdrop = new ColorRect { Color = new Color(0f, 0f, 0f, 0.62f), MouseFilter = MouseFilterEnum.Ignore };
        CardControl.Fill(backdrop);
        AddChild(backdrop);

        float left = 0.5f - (3 * CardWidth + 2 * Gap) / 2f;
        float top = 0.5f - CardHeight / 2f;
        for (int i = 0; i < 3; i++)
        {
            int tier = i + 1;
            var card = new CardControl();
            card.AnchorLeft = left + i * (CardWidth + Gap);
            card.AnchorRight = card.AnchorLeft + CardWidth;
            card.AnchorTop = top;
            card.AnchorBottom = top + CardHeight;
            card.Clicked += () => Choose(tier);
            _cards[i] = card;
            AddChild(card);

            var label = new Label
            {
                Text = $"{tier}",
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
                AnchorLeft = card.AnchorLeft,
                AnchorRight = card.AnchorRight,
                AnchorTop = top - 0.06f,
                AnchorBottom = top - 0.01f,
            };
            label.AddThemeFontSizeOverride("font_size", 36);
            _labels[i] = label;
            AddChild(label);
        }

        GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
            {
                Close();
                Cancelled?.Invoke();
                AcceptEvent();
            }
        };
    }

    public void Open(BoardAssets assets, string definitionId, int mana, bool debug)
    {
        var tiers = assets.Catalog.CardsById[definitionId].Tiers;
        for (int i = 0; i < 3; i++)
        {
            _affordable[i] = tiers[i].Cost <= mana;
            _cards[i].ShowCard(assets, definitionId, tiers[i], debug, false);
            _cards[i].Modulate = _affordable[i] ? Colors.White : new Color(1f, 1f, 1f, 0.3f);
            _labels[i].Modulate = _cards[i].Modulate;
        }

        Visible = true;
    }

    public void Close() => Visible = false;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || @event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        int tier = key.Keycode switch
        {
            Key.Key1 or Key.Kp1 => 1,
            Key.Key2 or Key.Kp2 => 2,
            Key.Key3 or Key.Kp3 => 3,
            _ => 0,
        };

        if (tier > 0)
        {
            Choose(tier);
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape)
        {
            Close();
            Cancelled?.Invoke();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Choose(int tier)
    {
        if (_affordable[tier - 1])
        {
            TierChosen?.Invoke(tier);
        }
    }
}
