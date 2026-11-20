using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.PlayerSystem.Gadgets;

/// <summary>
/// Shared wall-clipped throw trajectory math for thrown gadgets across any map geometry.
/// </summary>
internal static class GadgetThrowHelper
{
    private const float DirectionEpsilonSq = 0.0001f;
    private const float WallStopPullback = 10f;

    public static Vector2 ResolveLandingPoint(
        Vector2 origin,
        Vector2 target,
        float maxThrowDistance,
        float collisionRadius,
        IReadOnlyList<RectangleBounds>? walls)
    {
        Vector2 delta = target - origin;
        float distanceSq = delta.LengthSquared();
        if (distanceSq <= DirectionEpsilonSq || maxThrowDistance <= 0f)
        {
            return origin;
        }

        float distance = MathF.Sqrt(distanceSq);
        Vector2 direction = delta / distance;
        float clampedDistance = MathF.Min(distance, maxThrowDistance);

        if (walls is { Count: > 0 })
        {
            float hitDistance = VisionRaycaster.FindClosestWallHitDistance(origin, direction, clampedDistance, walls);
            float safeDistance = hitDistance < clampedDistance
                ? MathF.Max(0f, hitDistance - WallStopPullback)
                : clampedDistance;
            Vector2 candidate = origin + (direction * safeDistance);
            return CollisionHelper.ResolveWallCollision(candidate, collisionRadius, walls);
        }

        return origin + (direction * clampedDistance);
    }
}
