using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.States;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Shared guard vision cone raycast, alarm detection, and wall-sliding movement helpers.
/// </summary>
public static class GuardSensorHelper
{
    public const float DefaultVisionRange = 235f;
    public const float DefaultHalfAngleDegrees = 32f;

    public static bool TrySpotIntruder(
        Guard guard,
        IReadOnlyList<Vector2> patrolWaypoints,
        GameContext context,
        out IGuardState alertState)
    {
        if (!CanSeePlayer(guard, context))
        {
            alertState = null!;
            return false;
        }

        if (!context.IsAlarmTriggered)
        {
            context.IsAlarmTriggered = true;
            context.EventBus.Publish(new AlarmTriggeredEvent(context.PlayerPosition, $"SpottedBy:{guard.Id}"));
        }

        alertState = new AlertChaseState(guard, patrolWaypoints, context.PlayerPosition);
        return true;
    }

    public static bool CanSeePlayer(
        Guard guard,
        GameContext context,
        float visionRange = DefaultVisionRange,
        float halfAngleDegrees = DefaultHalfAngleDegrees)
    {
        Vector2 toPlayer = context.PlayerPosition - guard.Position;
        float distSq = toPlayer.LengthSquared();
        if (distSq > visionRange * visionRange) return false;
        if (distSq < 1f) return true;

        float rad = guard.RotationDegrees * (MathF.PI / 180f);
        Vector2 forward = new(MathF.Cos(rad), MathF.Sin(rad));
        float dot = Vector2.Dot(forward, Vector2.Normalize(toPlayer));
        float cosHalf = MathF.Cos(halfAngleDegrees * (MathF.PI / 180f));
        if (dot < cosHalf) return false;

        return context.Walls.Count == 0 ||
               CollisionHelper.HasLineOfSight(guard.Position, context.PlayerPosition, context.Walls);
    }

    public static void MoveWithCollision(Guard guard, Vector2 step, IReadOnlyList<RectangleBounds> walls)
    {
        guard.Position += step;
        if (walls.Count > 0)
        {
            guard.Position = CollisionHelper.ResolveWallCollision(guard.Position, CollisionHelper.DefaultActorRadius, walls);
        }
    }
}
