using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting;

public static class VisionRaycaster
{
    public const float DefaultFovDegrees = 70f;
    public const float MaxVisionDist = 240f;
    public const int DefaultRayCount = 32;

    private const float DegToRad = MathF.PI / 180f;
    private const float ParallelEpsilon = 0.00001f;
    private const float HalfMultiplier = 0.5f;

    public static IReadOnlyList<Vector2> ComputeVisionCone(
        Vector2 origin,
        float facingDegrees,
        IEnumerable<RectangleBounds> walls,
        float fovDegrees = DefaultFovDegrees,
        float maxDistance = MaxVisionDist,
        int rayCount = DefaultRayCount)
    {
        int safeRayCount = Math.Max(1, rayCount);
        float safeMaxDistance = Math.Max(0f, maxDistance);
        float startAngle = facingDegrees - (fovDegrees * HalfMultiplier);
        float angleStep = fovDegrees / safeRayCount;

        var coneVertices = new List<Vector2>(safeRayCount + 2) { origin };

        for (int i = 0; i <= safeRayCount; i++)
        {
            Vector2 direction = DirectionFromDegrees(startAngle + (angleStep * i));
            coneVertices.Add(CastRay(origin, direction, safeMaxDistance, walls));
        }

        return coneVertices;
    }

    public static Vector2 CastRay(Vector2 origin, Vector2 direction, float maxDistance, IEnumerable<RectangleBounds> walls)
    {
        if (maxDistance <= 0f || direction.LengthSquared() < ParallelEpsilon || IsOriginInsideAnyWall(origin, walls))
        {
            return origin;
        }

        Vector2 rayDirection = Vector2.Normalize(direction);
        float closestDistance = FindClosestWallHitDistance(origin, rayDirection, maxDistance, walls);
        return origin + (rayDirection * closestDistance);
    }

    public static bool TryIntersectRayWithSegment(
        Vector2 rayOrigin,
        Vector2 rayDirection,
        Vector2 segmentStart,
        Vector2 segmentEnd,
        out float hitDistance)
    {
        hitDistance = 0f;
        Vector2 segmentVector = segmentEnd - segmentStart;
        float denominator = CrossProduct2D(rayDirection, segmentVector);

        if (MathF.Abs(denominator) < ParallelEpsilon)
        {
            return false;
        }

        Vector2 originToSegment = segmentStart - rayOrigin;
        float rayDistance = CrossProduct2D(originToSegment, segmentVector) / denominator;
        float segmentFraction = CrossProduct2D(originToSegment, rayDirection) / denominator;

        if (rayDistance < 0f || segmentFraction < 0f || segmentFraction > 1f)
        {
            return false;
        }

        hitDistance = rayDistance;
        return true;
    }

    public static Vector2 DirectionFromDegrees(float angleDegrees)
    {
        float radians = angleDegrees * DegToRad;
        return new Vector2(MathF.Cos(radians), MathF.Sin(radians));
    }

    private static bool IsOriginInsideAnyWall(Vector2 origin, IEnumerable<RectangleBounds> walls)
    {
        foreach (var wall in walls)
        {
            if (wall.Width > 0f && wall.Height > 0f &&
                origin.X >= wall.X && origin.X <= wall.X + wall.Width &&
                origin.Y >= wall.Y && origin.Y <= wall.Y + wall.Height)
            {
                return true;
            }
        }

        return false;
    }

    private static float FindClosestWallHitDistance(Vector2 origin, Vector2 direction, float maxDistance, IEnumerable<RectangleBounds> walls)
    {
        float closestDistance = maxDistance;

        foreach (var wall in walls)
        {
            if (TryIntersectRayWithWall(origin, direction, wall, out float wallDistance) && wallDistance < closestDistance)
            {
                closestDistance = wallDistance;
            }
        }

        return closestDistance;
    }

    private static bool TryIntersectRayWithWall(Vector2 origin, Vector2 direction, RectangleBounds wall, out float closestWallDistance)
    {
        closestWallDistance = float.PositiveInfinity;
        if (wall.Width <= 0f || wall.Height <= 0f)
        {
            return false;
        }

        var topLeft = new Vector2(wall.X, wall.Y);
        var topRight = new Vector2(wall.X + wall.Width, wall.Y);
        var bottomRight = new Vector2(wall.X + wall.Width, wall.Y + wall.Height);
        var bottomLeft = new Vector2(wall.X, wall.Y + wall.Height);

        bool hitAnyEdge = false;
        hitAnyEdge |= CheckEdgeHit(origin, direction, topLeft, topRight, ref closestWallDistance);
        hitAnyEdge |= CheckEdgeHit(origin, direction, topRight, bottomRight, ref closestWallDistance);
        hitAnyEdge |= CheckEdgeHit(origin, direction, bottomRight, bottomLeft, ref closestWallDistance);
        hitAnyEdge |= CheckEdgeHit(origin, direction, bottomLeft, topLeft, ref closestWallDistance);

        return hitAnyEdge;
    }

    private static bool CheckEdgeHit(Vector2 origin, Vector2 direction, Vector2 edgeStart, Vector2 edgeEnd, ref float currentClosest)
    {
        if (!TryIntersectRayWithSegment(origin, direction, edgeStart, edgeEnd, out float hitDistance) || hitDistance >= currentClosest)
        {
            return false;
        }

        currentClosest = hitDistance;
        return true;
    }

    private static float CrossProduct2D(Vector2 a, Vector2 b) => (a.X * b.Y) - (a.Y * b.X);
}
