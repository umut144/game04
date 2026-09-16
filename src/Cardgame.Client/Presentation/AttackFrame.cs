using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// The attack highlight on a field the selected unit can reach: a red dashed
/// frame with solid corners. <see cref="Affected"/> adds a translucent fill
/// for the fields a hovered target's attack would hit.
/// </summary>
public partial class AttackFrame : Control
{
    private static readonly Color Red = new("E53935");
    private const float Inset = 6f;
    private const float Line = 3f;
    private const float Dash = 12f;
    private const float Corner = 22f;

    private bool _affected;

    public AttackFrame()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        Resized += QueueRedraw;
    }

    public bool Affected
    {
        get => _affected;
        set
        {
            _affected = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.One * Inset, Size - Vector2.One * 2 * Inset);
        if (_affected)
        {
            DrawRect(rect, new Color(Red, 0.22f));
        }

        var a = rect.Position;
        var b = rect.Position + new Vector2(rect.Size.X, 0);
        var c = rect.End;
        var d = rect.Position + new Vector2(0, rect.Size.Y);
        foreach (var (from, to) in new[] { (a, b), (b, c), (c, d), (d, a) })
        {
            DrawDashedLine(from, to, Red, Line, Dash);
        }

        foreach (var (corner, h, v) in new[]
        {
            (a, Vector2.Right, Vector2.Down),
            (b, Vector2.Left, Vector2.Down),
            (c, Vector2.Left, Vector2.Up),
            (d, Vector2.Right, Vector2.Up),
        })
        {
            DrawLine(corner, corner + h * Corner, Red, Line * 2);
            DrawLine(corner, corner + v * Corner, Red, Line * 2);
        }
    }
}
