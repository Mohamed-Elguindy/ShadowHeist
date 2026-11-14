using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Shared low-level 3D geometric helpers for ground shadows, stance rings, outlined boxes, and laser beams.
/// </summary>
public static class Raylib3DPrimitives
{
    private const float ShadowHeight = 0.5f;
    private const float ShadowElevationY = 0.4f;
    private const float RingElevationY = 1.3f;
    private const int DefaultCylinderSides = 12;

    public static void DrawGroundShadow(Vector2 position, float radius)
    {
        var center = new Vector3(position.X, ShadowElevationY, position.Y);
        Raylib.DrawCylinder(center, radius, radius, ShadowHeight, DefaultCylinderSides, Viewport3DConfig.ShadowColor);
    }

    public static void DrawGroundRing(Vector2 position, Color ringColor)
    {
        DrawGroundShadow(position, Viewport3DConfig.ShadowRadius);
        var ringCenter = new Vector3(position.X, RingElevationY, position.Y);
        float ringRadius = Viewport3DConfig.ShadowRadius + 2f;
        Raylib.DrawCylinderWires(ringCenter, ringRadius, ringRadius, 0.4f, 20, ringColor);
    }

    public static void DrawOutlinedCube(Vector3 center, Vector3 size, Color fillColor, Color outlineColor)
    {
        Raylib.DrawCube(center, size.X, size.Y, size.Z, fillColor);
        Raylib.DrawCubeWires(center, size.X, size.Y, size.Z, outlineColor);
    }

    public static void DrawVolumetricBeam(
        Vector3 start,
        Vector3 end,
        float glowRadius,
        float coreRadius,
        Color glowColor,
        Color coreColor)
    {
        Raylib.DrawCylinderEx(start, end, glowRadius, glowRadius, 8, glowColor);
        Raylib.DrawCylinderEx(start, end, coreRadius, coreRadius, 6, coreColor);
    }
}
