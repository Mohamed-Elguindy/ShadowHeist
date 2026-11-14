using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Shared low-level 3D geometric helpers to avoid duplicating shadow disc, outlined box, and laser beam rendering.
/// </summary>
public static class Raylib3DPrimitives
{
    private const float ShadowHeight = 0.5f;
    private const float ShadowElevationY = 0.4f;
    private const int DefaultCylinderSides = 12;

    public static void DrawGroundShadow(Vector2 position, float radius)
    {
        var center = new Vector3(position.X, ShadowElevationY, position.Y);
        Raylib.DrawCylinder(center, radius, radius, ShadowHeight, DefaultCylinderSides, Viewport3DConfig.ShadowColor);
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
