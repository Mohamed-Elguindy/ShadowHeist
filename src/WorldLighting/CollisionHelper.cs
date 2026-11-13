using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting;

public static class CollisionHelper
{
    public const float DefaultActorRadius = 18f;
    private const float MinDistanceSquared = 0.0001f;

    public static Vector2 ResolveWallCollision(
        Vector2 actorPosition,
        float actorRadius,
        IEnumerable<RectangleBounds> walls)
    {
        if (actorRadius <= 0f)
        {
            return actorPosition;
        }

        Vector2 resolvedPosition = actorPosition;
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (var wall in walls)
            {
                resolvedPosition = ResolveSingleWallCollision(resolvedPosition, actorRadius, wall);
            }
        }

        return resolvedPosition;
    }

    public static bool HasLineOfSight(Vector2 from, Vector2 to, IReadOnlyList<RectangleBounds> walls)
    {
        Vector2 delta = to - from;
        float dist = delta.Length();
        if (dist <= 0.001f) return true;
        float hit = CastRayAgainstWalls(from, delta / dist, dist, walls);
        return hit >= dist - 0.5f;
    }

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

    public static Vector2 ResolveSingleWallCollision(
        Vector2 actorPosition,
        float actorRadius,
        RectangleBounds wall)
    {
        if (IsInvalidInput(actorRadius, wall))
        {
            return actorPosition;
        }

        Vector2 closestPointOnWall = ClampToWall(actorPosition, wall);
        Vector2 offsetFromWall = actorPosition - closestPointOnWall;
        float distanceSquared = offsetFromWall.LengthSquared();

        if (distanceSquared >= actorRadius * actorRadius)
        {
            return actorPosition;
        }

        if (distanceSquared > MinDistanceSquared)
        {
            return closestPointOnWall + (Vector2.Normalize(offsetFromWall) * actorRadius);
        }

        return PushToNearestWallEdge(actorPosition, actorRadius, wall);
    }

    private static bool IsInvalidInput(float actorRadius, RectangleBounds wall)
    {
        return actorRadius <= 0f || wall.Width <= 0f || wall.Height <= 0f;
    }

    private static Vector2 ClampToWall(Vector2 actorPosition, RectangleBounds wall)
    {
        var minBounds = new Vector2(wall.X, wall.Y);
        var maxBounds = new Vector2(wall.X + wall.Width, wall.Y + wall.Height);
        return Vector2.Clamp(actorPosition, minBounds, maxBounds);
    }

    private static Vector2 PushToNearestWallEdge(
        Vector2 actorPosition,
        float actorRadius,
        RectangleBounds wall)
    {
        float wallRight = wall.X + wall.Width;
        float wallBottom = wall.Y + wall.Height;

        float distToLeft = MathF.Abs(actorPosition.X - wall.X);
        float distToRight = MathF.Abs(wallRight - actorPosition.X);
        float distToTop = MathF.Abs(actorPosition.Y - wall.Y);
        float distToBottom = MathF.Abs(wallBottom - actorPosition.Y);

        float nearestHorizontalDist = MathF.Min(distToLeft, distToRight);
        float nearestVerticalDist = MathF.Min(distToTop, distToBottom);

        if (nearestHorizontalDist < nearestVerticalDist)
        {
            float resolvedX = distToLeft < distToRight
                ? wall.X - actorRadius
                : wallRight + actorRadius;
            return new Vector2(resolvedX, actorPosition.Y);
        }

        float resolvedY = distToTop < distToBottom
            ? wall.Y - actorRadius
            : wallBottom + actorRadius;
        return new Vector2(actorPosition.X, resolvedY);
    }
}
