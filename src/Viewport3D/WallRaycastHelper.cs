using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Pure 2D ray-AABB intersection helper so guard flashlight cones clip cleanly against bank walls.
/// </summary>
public static class WallRaycastHelper
{
    public static float CastRayAgainstWalls(
        Vector2 origin,
        Vector2 direction,
        float maxDistance,
        IReadOnlyList<RectangleBounds> walls)
    {
        float closest = maxDistance;
        foreach (RectangleBounds wall in walls)
        {
            if (TryIntersectRayAabb(origin, direction, wall, out float dist) && dist < closest)
            {
                closest = dist;
            }
        }

        return closest;
    }

    private static bool TryIntersectRayAabb(Vector2 origin, Vector2 dir, RectangleBounds box, out float hitDistance)
    {
        hitDistance = float.MaxValue;
        float tMin = 0f;
        float tMax = float.MaxValue;

        if (!ClipAxis(origin.X, dir.X, box.X, box.X + box.Width, ref tMin, ref tMax) ||
            !ClipAxis(origin.Y, dir.Y, box.Y, box.Y + box.Height, ref tMin, ref tMax))
        {
            return false;
        }

        hitDistance = tMin;
        return tMin >= 0f;
    }

    private static bool ClipAxis(float origin, float dir, float min, float max, ref float tMin, ref float tMax)
    {
        if (MathF.Abs(dir) < 0.00001f)
        {
            return origin >= min && origin <= max;
        }

        float inv = 1f / dir;
        float t1 = (min - origin) * inv;
        float t2 = (max - origin) * inv;
        if (t1 > t2) (t1, t2) = (t2, t1);

        tMin = MathF.Max(tMin, t1);
        tMax = MathF.Min(tMax, t2);
        return tMin <= tMax;
    }
}
