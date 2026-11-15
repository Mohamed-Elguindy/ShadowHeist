using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders warm floor lamp glow pools and wall-clipped 3D projected vision cone polygons.
/// </summary>
public static class VisionCone3DRenderer
{
    private const float LampPoolElevationY = 0.6f;
    private const float OuterPoolRadius = 95f;
    private const float OuterPoolHeight = 0.4f;
    private const int OuterPoolSlices = 20;
    private const float InnerPoolRadius = 46f;
    private const float InnerPoolHeight = 0.6f;
    private const int InnerPoolSlices = 16;
    private const float LampBulbElevationY = 66f;
    private const float LampBulbRadius = 7f;
    private const float PlayerConeGroundY = 2.3f;
    private const float PlayerConeSourceY = 34f;
    private const float BeamEdgeAlphaMultiplier = 0.4f;

    private static readonly Color OuterLampPoolColor = new(255, 214, 120, 34);
    private static readonly Color InnerLampPoolColor = new(255, 224, 150, 56);
    private static readonly Color LampBulbGlowColor = new(255, 236, 168, 225);
    private static readonly Color PlayerConeFillColor = new(56, 225, 255, 36);
    private static readonly Color PlayerConeEdgeColor = new(110, 235, 255, 115);

    public static void DrawWarmLampGlows()
    {
        foreach (FurnitureSpec spec in BankFurnitureLayout.LobbyFurniture)
        {
            if (!spec.EmitsWarmGlow)
            {
                continue;
            }

            var center = new Vector3(spec.Position.X, LampPoolElevationY, spec.Position.Y);
            Raylib.DrawCylinder(center, OuterPoolRadius, OuterPoolRadius, OuterPoolHeight, OuterPoolSlices, OuterLampPoolColor);
            Raylib.DrawCylinder(center, InnerPoolRadius, InnerPoolRadius, InnerPoolHeight, InnerPoolSlices, InnerLampPoolColor);
            Raylib.DrawSphere(new Vector3(spec.Position.X, LampBulbElevationY, spec.Position.Y), LampBulbRadius, LampBulbGlowColor);
        }
    }

    public static void DrawPlayerVisionCone(IReadOnlyList<Vector2> coneVertices) =>
        DrawProjectedVisionPolygon(
            coneVertices,
            groundY: PlayerConeGroundY,
            sourceY: PlayerConeSourceY,
            fill: PlayerConeFillColor,
            edge: PlayerConeEdgeColor);

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
        var beamEdgeColor = new Color(edge.R, edge.G, edge.B, (byte)(edge.A * BeamEdgeAlphaMultiplier));

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
