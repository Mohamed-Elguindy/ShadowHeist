using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.States;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Shared guard vision cone raycast, close-proximity awareness circle, and progressive detection meter helpers (Ticket #22 — Hatem).
/// </summary>
public static class GuardSensorHelper
{
    public const float DefaultVisionRange = 235f;
    public const float DefaultHalfAngleDegrees = 32f;
    public const float AlarmVisionMultiplier = 1.25f;
    public const float AlarmHalfAngleDegrees = 48f;
    public const float ProximityAwarenessRadius = 112f;

    private const float MinVisibleDistanceSquared = 1f;
    private const float DefaultFrameDeltaSeconds = 0.05f;

    public static float ResolveVisionRange(bool isAlarmTriggered) =>
        isAlarmTriggered ? DefaultVisionRange * AlarmVisionMultiplier : DefaultVisionRange;

    public static float ResolveHalfAngleDegrees(bool isAlarmTriggered) =>
        isAlarmTriggered ? AlarmHalfAngleDegrees : DefaultHalfAngleDegrees;

    public static bool TrySpotIntruder(
        Guard guard,
        IReadOnlyList<Vector2> patrolWaypoints,
        GameContext context,
        out IGuardState alertState) =>
        TrySpotIntruder(guard, patrolWaypoints, context, DefaultFrameDeltaSeconds, out alertState);

    public static bool TrySpotIntruder(
        Guard guard,
        IReadOnlyList<Vector2> patrolWaypoints,
        GameContext context,
        float deltaTime,
        out IGuardState alertState)
    {
        float activeVisionRange = ResolveVisionRange(context.IsAlarmTriggered);
        float activeHalfAngle = ResolveHalfAngleDegrees(context.IsAlarmTriggered);

        bool inCone = IsPlayerInForwardCone(guard, context, activeVisionRange, activeHalfAngle);
        bool inProximity = IsPlayerInProximityCircle(guard, context);

        if ((inCone || inProximity) && context.IsAlarmTriggered)
        {
            guard.Detection.FillInstantly();
        }
        else
        {
            float distance = Vector2.Distance(guard.Position, context.PlayerPosition);
            guard.Detection.Update(deltaTime, inCone, inProximity, distance, activeVisionRange, context.IsPlayerSneaking);
        }

        if (!guard.Detection.IsFullyDetected)
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
        float? visionRange = null,
        float? halfAngleDegrees = null)
    {
        float activeRange = visionRange ?? ResolveVisionRange(context.IsAlarmTriggered);
        float activeHalfAngle = halfAngleDegrees ?? ResolveHalfAngleDegrees(context.IsAlarmTriggered);

        return IsPlayerInProximityCircle(guard, context) ||
               IsPlayerInForwardCone(guard, context, activeRange, activeHalfAngle);
    }

    public static bool IsPlayerInProximityCircle(
        Guard guard,
        GameContext context,
        float proximityRadius = ProximityAwarenessRadius)
    {
        float distSq = Vector2.DistanceSquared(guard.Position, context.PlayerPosition);
        if (distSq > proximityRadius * proximityRadius)
        {
            return false;
        }

        return HasUnobstructedLineOfSight(guard.Position, context.PlayerPosition, context.Walls);
    }

    public static bool IsPlayerInForwardCone(
        Guard guard,
        GameContext context,
        float visionRange,
        float halfAngleDegrees)
    {
        Vector2 toPlayer = context.PlayerPosition - guard.Position;
        float distSq = toPlayer.LengthSquared();
        if (distSq > visionRange * visionRange) return false;
        if (distSq < MinVisibleDistanceSquared) return true;

        float rad = guard.RotationDegrees * HeistConstants.DegToRad;
        Vector2 forward = new(MathF.Cos(rad), MathF.Sin(rad));
        float dot = Vector2.Dot(forward, Vector2.Normalize(toPlayer));
        float cosHalf = MathF.Cos(halfAngleDegrees * HeistConstants.DegToRad);
        if (dot < cosHalf) return false;

        return HasUnobstructedLineOfSight(guard.Position, context.PlayerPosition, context.Walls);
    }

    private static bool HasUnobstructedLineOfSight(
        Vector2 origin,
        Vector2 target,
        IReadOnlyList<RectangleBounds> walls) =>
        walls.Count == 0 || CollisionHelper.HasLineOfSight(origin, target, walls);
}
