using System;
using System.Collections.Generic;
using Cardgame.Assets;
using Godot;

namespace Cardgame.Client.Presentation;

/// <summary>
/// Draws one asset geometry inside this control: one meter of game04 geometry
/// is <see cref="PixelsPerMeter"/> × <see cref="Fill"/> pixels, and the
/// asset's pivot (bottom centre) sits so that an asset exactly as tall as the
/// control is centred in it — the space <see cref="Fill"/> gives up is shared
/// evenly around the asset.
/// </summary>
public partial class AssetView : Control
{
    private ArrayMesh? _mesh;

    public Func<float> PixelsPerMeter { get; set; } = () => 1f;

    public float Fill { get; set; } = 1f;

    public AssetView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public void Display(AssetGeometry geometry, Func<AssetPart, Color> colourOf)
    {
        _mesh = BuildMesh(geometry, colourOf);
        QueueRedraw();
    }

    public void Clear()
    {
        _mesh = null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_mesh is null)
        {
            return;
        }

        float scale = PixelsPerMeter() * Fill;
        var transform = new Transform2D(
            new Vector2(scale, 0f),
            new Vector2(0f, -scale),
            new Vector2(Size.X / 2f, Size.Y - Size.Y * (1f - Fill) / 2f));
        DrawMesh(_mesh, null!, transform);
    }

    private static ArrayMesh BuildMesh(AssetGeometry geometry, Func<AssetPart, Color> colourOf)
    {
        var vertices = new List<Vector2>();
        var colours = new List<Color>();
        var uvs = new List<Vector2>();
        var indices = new List<int>();
        foreach (var part in geometry.Parts)
        {
            int offset = vertices.Count;
            var colour = colourOf(part);
            for (int i = 0; i < part.Vertices.Length; i += 2)
            {
                vertices.Add(new Vector2(part.Vertices[i], part.Vertices[i + 1]));
                colours.Add(colour);
                uvs.Add(new Vector2(part.Vertices[i], part.Vertices[i + 1]));
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
        // The asset's own meters, for shaders that mask by position.
        arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
