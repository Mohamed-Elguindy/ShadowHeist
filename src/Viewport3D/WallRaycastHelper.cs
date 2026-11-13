using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Viewport-facing facade forwarding 2D ray-AABB wall clipping to the shared CollisionHelper.
/// </summary>
public static class WallRaycastHelper
{
    public static float CastRayAgainstWalls(
        Vector2 origin,
        Vector2 direction,
        float maxDistance,
        IReadOnlyList<RectangleBounds> walls) =>
        CollisionHelper.CastRayAgainstWalls(origin, direction, maxDistance, walls);
}
