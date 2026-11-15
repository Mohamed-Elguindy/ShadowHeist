using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting;

public static class CollisionHelper
{
    public const float DefaultActorRadius = 18f;
    private const float MinDistanceSquared = 0.0001f;
    private const float MinLineOfSightDistance = 0.001f;
    private const float LineOfSightHitTolerance = 0.5f;
    private const int CollisionResolutionPasses = 2;

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
        for (int pass = 0; pass < CollisionResolutionPasses; pass++)
        {
            foreach (RectangleBounds wall in walls)
            {
                resolvedPosition = ResolveSingleWallCollision(resolvedPosition, actorRadius, wall);
            }
        }

        return resolvedPosition;
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

    public static bool HasLineOfSight(Vector2 origin, Vector2 target, IReadOnlyList<RectangleBounds> walls)
    {
        Vector2 delta = target - origin;
        float distance = delta.Length();
        if (distance <= MinLineOfSightDistance)
        {
            return true;
        }

        float hitDistance = CastRayAgainstWalls(origin, delta / distance, distance, walls);
        return hitDistance >= distance - LineOfSightHitTolerance;
    }

    public static float CastRayAgainstWalls(
        Vector2 origin,
        Vector2 rayDirection,
        float maxDistance,
        IReadOnlyList<RectangleBounds> walls) =>
        VisionRaycaster.FindClosestWallHitDistance(origin, rayDirection, maxDistance, walls);

    private static bool IsInvalidInput(float actorRadius, RectangleBounds wall) =>
        actorRadius <= 0f || wall.Width <= 0f || wall.Height <= 0f;

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

        float distanceToLeft = MathF.Abs(actorPosition.X - wall.X);
        float distanceToRight = MathF.Abs(wallRight - actorPosition.X);
        float distanceToTop = MathF.Abs(actorPosition.Y - wall.Y);
        float distanceToBottom = MathF.Abs(wallBottom - actorPosition.Y);

        float nearestHorizontalDistance = MathF.Min(distanceToLeft, distanceToRight);
        float nearestVerticalDistance = MathF.Min(distanceToTop, distanceToBottom);

        if (nearestHorizontalDistance < nearestVerticalDistance)
        {
            float resolvedX = distanceToLeft < distanceToRight
                ? wall.X - actorRadius
                : wallRight + actorRadius;
            return new Vector2(resolvedX, actorPosition.Y);
        }

        float resolvedY = distanceToTop < distanceToBottom
            ? wall.Y - actorRadius
            : wallBottom + actorRadius;
        return new Vector2(actorPosition.X, resolvedY);
    }
}
