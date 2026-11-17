using System.Numerics;

namespace ShadowHeist.Core;

/// <summary>
/// Zero-allocation 2D ray-AABB visibility helpers shared across core subsystems.
/// </summary>
public static class Geometry2D
{
    private const float MinSegmentLength = 0.001f;
    private const float TargetSurfaceMargin = 0.5f;
    private const float ParallelDirectionEpsilon = 0.00001f;

    public static bool HasLineOfSight(
        Vector2 origin,
        Vector2 target,
        IReadOnlyList<RectangleBounds> obstacles,
        RectangleBounds? ignoredObstacle = null)
    {
        Vector2 delta = target - origin;
        float distance = delta.Length();
        if (distance <= MinSegmentLength)
        {
            return true;
        }

        Vector2 direction = delta / distance;
        float reachThreshold = distance - TargetSurfaceMargin;

        for (int i = 0; i < obstacles.Count; i++)
        {
            RectangleBounds wall = obstacles[i];
            if (ignoredObstacle.HasValue && wall == ignoredObstacle.Value)
            {
                continue;
            }

            if (TryIntersectRayAabb(origin, direction, wall, out float hitDistance) && hitDistance < reachThreshold)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryIntersectRayAabb(
        Vector2 origin,
        Vector2 direction,
        RectangleBounds box,
        out float hitDistance)
    {
        hitDistance = float.MaxValue;
        float nearDistance = 0f;
        float farDistance = float.MaxValue;

        if (!ClipAxis(origin.X, direction.X, box.X, box.X + box.Width, ref nearDistance, ref farDistance) ||
            !ClipAxis(origin.Y, direction.Y, box.Y, box.Y + box.Height, ref nearDistance, ref farDistance))
        {
            return false;
        }

        hitDistance = nearDistance;
        return nearDistance >= 0f;
    }

    private static bool ClipAxis(
        float origin,
        float direction,
        float minBound,
        float maxBound,
        ref float nearDistance,
        ref float farDistance)
    {
        if (MathF.Abs(direction) < ParallelDirectionEpsilon)
        {
            return origin >= minBound && origin <= maxBound;
        }

        float inverseDirection = 1f / direction;
        float entryDistance = (minBound - origin) * inverseDirection;
        float exitDistance = (maxBound - origin) * inverseDirection;
        if (entryDistance > exitDistance)
        {
            (entryDistance, exitDistance) = (exitDistance, entryDistance);
        }

        nearDistance = MathF.Max(nearDistance, entryDistance);
        farDistance = MathF.Min(farDistance, exitDistance);
        return nearDistance <= farDistance;
    }

    public static float DistanceToBounds(Vector2 point, RectangleBounds box)
    {
        float dx = MathF.Max(0f, MathF.Max(box.X - point.X, point.X - (box.X + box.Width)));
        float dy = MathF.Max(0f, MathF.Max(box.Y - point.Y, point.Y - (box.Y + box.Height)));
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    public static bool SegmentIntersectsBounds(Vector2 start, Vector2 end, RectangleBounds box, float pad = 0f)
    {
        float minT = 0f;
        float maxT = 1f;
        Vector2 delta = end - start;
        return ClipAxis(start.X, delta.X, box.X - pad, box.X + box.Width + pad, ref minT, ref maxT)
            && ClipAxis(start.Y, delta.Y, box.Y - pad, box.Y + box.Height + pad, ref minT, ref maxT);
    }
}
