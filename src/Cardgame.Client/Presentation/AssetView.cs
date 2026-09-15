using System;
using Cardgame.Assets;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// Draws one PolyTools asset inside this control: the asset's pivot sits at
/// the bottom centre, and one meter of game04 geometry is
/// <see cref="PixelsPerMeter"/> pixels, the same for everything on the board.
/// </summary>
public partial class AssetView : Control
{
    private ArrayMesh? _mesh;

    public Func<float> PixelsPerMeter { get; set; } = () => 1f;

    public AssetView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public void Display(AssetGeometry geometry, Color fill, Color stroke)
    {
        _mesh = BuildMesh(geometry, fill, stroke);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_mesh is null)
        {
            return;
        }

        float scale = PixelsPerMeter();
        var transform = new Transform2D(
            new Vector2(scale, 0f),
            new Vector2(0f, -scale),
            new Vector2(Size.X / 2f, Size.Y));
        DrawMesh(_mesh, null!, transform);
    }

    private static ArrayMesh BuildMesh(AssetGeometry geometry, Color fill, Color stroke)
    {
        var vertices = new System.Collections.Generic.List<Vector2>();
        var colours = new System.Collections.Generic.List<Color>();
        var indices = new System.Collections.Generic.List<int>();
        foreach (var part in geometry.Parts)
        {
            int offset = vertices.Count;
            var colour = part.Kind == AssetPartKind.Fill ? fill : stroke;
            for (int i = 0; i < part.Vertices.Length; i += 2)
            {
                vertices.Add(new Vector2(part.Vertices[i], part.Vertices[i + 1]));
                colours.Add(colour);
            }

            foreach (int index in part.Indices)
            {
                indices.Add(offset + index);
            }
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colours.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
