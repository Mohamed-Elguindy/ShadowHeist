using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders a subtle architectural blueprint grid around the heist facility to ground the 3D building in the scene.
/// </summary>
internal static class BlueprintGrid3DRenderer
{
    private const float GridY = -Viewport3DConfig.FloorThickness;
    private const float GridMargin = 384f;
    private const float GridStep = 128f;
    private const float BorderHeight = 1f;

    private static readonly Color GridLineColor = new(30, 44, 70, 52);
    private static readonly Color PerimeterLineColor = new(56, 189, 248, 45);

    public static void Draw()
    {
        float minX = -GridMargin;
        float maxX = Viewport3DConfig.MapWidth + GridMargin;
        float minZ = -GridMargin;
        float maxZ = Viewport3DConfig.MapHeight + GridMargin;

        for (float x = minX; x <= maxX; x += GridStep)
        {
            Raylib.DrawLine3D(new Vector3(x, GridY, minZ), new Vector3(x, GridY, maxZ), GridLineColor);
        }

        for (float z = minZ; z <= maxZ; z += GridStep)
        {
            Raylib.DrawLine3D(new Vector3(minX, GridY, z), new Vector3(maxX, GridY, z), GridLineColor);
        }

        var center = new Vector3(Viewport3DConfig.MapWidth * 0.5f, BorderHeight * 0.5f, Viewport3DConfig.MapHeight * 0.5f);
        Raylib.DrawCubeWires(center, Viewport3DConfig.MapWidth, BorderHeight, Viewport3DConfig.MapHeight, PerimeterLineColor);
    }
}
