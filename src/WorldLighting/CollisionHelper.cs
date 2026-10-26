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
        foreach (var wall in walls)
        {
            resolvedPosition = ResolveSingleWallCollision(resolvedPosition, actorRadius, wall);
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
