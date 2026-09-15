namespace Cardgame.Assets;

using System.Numerics;

/// <summary>
/// A 2D affine transform, columns (A,B) and (C,D) plus translation (E,F):
/// x' = A·x + C·y + E, y' = B·x + D·y + F.
/// </summary>
public readonly record struct Affine2(double A, double B, double C, double D, double E, double F)
{
    public static Affine2 Identity { get; } = new(1, 0, 0, 1, 0, 0);

    /// <summary>T(position) · R(rotation) · S(scale), counter-clockwise radians.</summary>
    public static Affine2 FromTransform(double x, double y, double rotation, double scaleX, double scaleY)
    {
        double cos = Math.Cos(rotation);
        double sin = Math.Sin(rotation);
        return new Affine2(cos * scaleX, sin * scaleX, -sin * scaleY, cos * scaleY, x, y);
    }

    /// <summary>This transform applied after <paramref name="inner"/>.</summary>
    public Affine2 Then(Affine2 inner) => new(
        A * inner.A + C * inner.B,
        B * inner.A + D * inner.B,
        A * inner.C + C * inner.D,
        B * inner.C + D * inner.D,
        A * inner.E + C * inner.F + E,
        B * inner.E + D * inner.F + F);

    public (double X, double Y) Apply(double x, double y) => (A * x + C * y + E, B * x + D * y + F);
}
