using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Pure static Circle-vs-AABB collision resolver (Ticket #8 - Ali).
/// Pushes circular entities out of axis-aligned wall rectangles while allowing smooth wall sliding.
/// </summary>
public static class CollisionHelper
{
    public const float DefaultActorRadius = 18f;
    private const float DistanceEpsilon = 0.0001f;

    /// <summary>
    /// Resolves collisions between a circular actor and all wall bounds, returning the corrected position.
    /// </summary>
    public static Vector2 ResolveWallCollision(
        Vector2 position,
        float radius,
        IEnumerable<RectangleBounds> walls)
    {
        ArgumentNullException.ThrowIfNull(walls);

        if (radius <= 0f)
        {
            return position;
        }

        Vector2 resolvedPosition = position;
        foreach (var wall in walls)
        {
            resolvedPosition = ResolveSingleWallCollision(resolvedPosition, radius, wall);
        }

        return resolvedPosition;
    }

    /// <summary>
    /// Resolves collision between a circular actor and a single axis-aligned bounding box (AABB).
    /// </summary>
    public static Vector2 ResolveSingleWallCollision(
        Vector2 position,
        float radius,
        RectangleBounds wall)
    {
        if (radius <= 0f || wall.Width <= 0f || wall.Height <= 0f)
        {
            return position;
        }

        float wallRight = wall.X + wall.Width;
        float wallBottom = wall.Y + wall.Height;

        float closestX = Math.Clamp(position.X, wall.X, wallRight);
        float closestY = Math.Clamp(position.Y, wall.Y, wallBottom);

        float deltaX = position.X - closestX;
        float deltaY = position.Y - closestY;
        float distanceSquared = (deltaX * deltaX) + (deltaY * deltaY);
        float radiusSquared = radius * radius;

        if (distanceSquared >= radiusSquared)
        {
            return position;
        }

        // Standard overlap: circle center is outside the wall rectangle
        if (distanceSquared > DistanceEpsilon)
        {
            float distance = MathF.Sqrt(distanceSquared);
            float penetrationDepth = radius - distance;
            float normalX = deltaX / distance;
            float normalY = deltaY / distance;

            return new Vector2(
                position.X + (normalX * penetrationDepth),
                position.Y + (normalY * penetrationDepth));
        }

        // Deep overlap: circle center is inside the wall rectangle; push out toward nearest edge
        float distToLeft = MathF.Abs(position.X - wall.X);
        float distToRight = MathF.Abs(wallRight - position.X);
        float distToTop = MathF.Abs(position.Y - wall.Y);
        float distToBottom = MathF.Abs(wallBottom - position.Y);

        float minHorizontal = MathF.Min(distToLeft, distToRight);
        float minVertical = MathF.Min(distToTop, distToBottom);

        if (minHorizontal < minVertical)
        {
            float resolvedX = distToLeft < distToRight
                ? wall.X - radius
                : wallRight + radius;
            return new Vector2(resolvedX, position.Y);
        }
        else
        {
            float resolvedY = distToTop < distToBottom
                ? wall.Y - radius
                : wallBottom + radius;
            return new Vector2(position.X, resolvedY);
        }
    }
}
