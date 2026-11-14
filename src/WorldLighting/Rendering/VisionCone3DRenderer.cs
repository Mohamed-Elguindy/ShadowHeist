using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders warm floor lamp glow pools and wall-clipped 3D projected vision cone polygons.
/// </summary>
public static class VisionCone3DRenderer
{
    public static void DrawWarmLampGlows()
    {
        foreach (FurnitureSpec spec in BankFurnitureLayout.LobbyFurniture)
        {
            if (!spec.EmitsWarmGlow)
            {
                continue;
            }

            var center = new Vector3(spec.Position.X, 0.6f, spec.Position.Y);
            Raylib.DrawCylinder(center, 95f, 95f, 0.4f, 20, new Color(255, 214, 120, 34));
            Raylib.DrawCylinder(center, 46f, 46f, 0.6f, 16, new Color(255, 224, 150, 56));
            Raylib.DrawSphere(new Vector3(spec.Position.X, 66f, spec.Position.Y), 7f, new Color(255, 236, 168, 225));
        }
    }

    public static void DrawPlayerVisionCone(IReadOnlyList<Vector2> coneVertices) =>
        DrawProjectedVisionPolygon(
            coneVertices,
            groundY: 2.3f,
            sourceY: 34f,
            fill: new Color(56, 225, 255, 36),
            edge: new Color(110, 235, 255, 115));

    public static void DrawProjectedVisionPolygon(
        IReadOnlyList<Vector2> vertices,
        float groundY,
        float sourceY,
        Color fill,
        Color edge)
    {
        if (vertices.Count < 3)
        {
            return;
        }

        var apex = new Vector3(vertices[0].X, groundY, vertices[0].Y);
        var beamOrigin = new Vector3(vertices[0].X, sourceY, vertices[0].Y);
        var beamEdgeColor = new Color(edge.R, edge.G, edge.B, (byte)(edge.A * 0.4f));

        for (int i = 1; i < vertices.Count - 1; i++)
        {
            var prev = new Vector3(vertices[i].X, groundY, vertices[i].Y);
            var hit = new Vector3(vertices[i + 1].X, groundY, vertices[i + 1].Y);
            Raylib.DrawTriangle3D(apex, hit, prev, fill);
            Raylib.DrawTriangle3D(apex, prev, hit, fill);
            Raylib.DrawLine3D(prev, hit, edge);
        }

        var first = new Vector3(vertices[1].X, groundY, vertices[1].Y);
        var last = new Vector3(vertices[^1].X, groundY, vertices[^1].Y);
        Raylib.DrawLine3D(apex, first, edge);
        Raylib.DrawLine3D(apex, last, edge);
        Raylib.DrawLine3D(beamOrigin, first, beamEdgeColor);
        Raylib.DrawLine3D(beamOrigin, last, beamEdgeColor);
    }
}
