using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;

namespace ShadowHeist.WorldLighting;

public sealed class LaserTripwire
{
    public const float DefaultMaxLaserLength = 1280f;
    private const float SegmentLengthEpsilon = 0.0001f;
    private const float MaxContinuousStepDistance = 96f;
    private const string AlarmReasonPrefix = "LaserTripwire:";

    private Vector2? _previousActorPosition;

    public string Id { get; }
    public Vector2 Origin { get; }
    public Vector2 Direction { get; }
    public float MaxLength { get; }
    public bool IsActive { get; set; } = true;
    public Vector2 BeamEnd { get; private set; }
    public bool HasTriggeredAlarm { get; private set; }

    public LaserTripwire(
        string id,
        Vector2 origin,
        Vector2 direction,
        float maxLength = DefaultMaxLaserLength)
    {
        Id = id;
        Origin = origin;
        Direction = direction.LengthSquared() > SegmentLengthEpsilon
            ? Vector2.Normalize(direction)
            : Vector2.UnitX;
        MaxLength = maxLength;
        BeamEnd = origin;
    }

    public void Update(
        GameContext context,
        float actorRadius = CollisionHelper.DefaultActorRadius,
        IReadOnlyList<RectangleBounds>? walls = null)
    {
        if (!IsActive)
        {
            BeamEnd = Origin;
            _previousActorPosition = context.PlayerPosition;
            return;
        }

        BeamEnd = VisionRaycaster.CastRay(Origin, Direction, MaxLength, walls ?? context.Walls);

        if (DoesActorIntersectBeam(context.PlayerPosition, actorRadius) ||
            DidActorPassThroughBeam(_previousActorPosition, context.PlayerPosition, actorRadius))
        {
            TriggerAlarm(context);
        }

        _previousActorPosition = context.PlayerPosition;
    }

    public void Reset()
    {
        HasTriggeredAlarm = false;
        IsActive = true;
        BeamEnd = Origin;
        _previousActorPosition = null;
    }

    private void TriggerAlarm(GameContext context)
    {
        if (HasTriggeredAlarm)
        {
            return;
        }

        HasTriggeredAlarm = true;
        context.IsAlarmTriggered = true;
        context.EventBus.Publish(new AlarmTriggeredEvent(context.PlayerPosition, $"{AlarmReasonPrefix}{Id}"));
    }

    private bool DoesActorIntersectBeam(Vector2 actorPosition, float actorRadius)
    {
        if (actorRadius <= 0f)
        {
            return false;
        }

        Vector2 closestPoint = ClosestPointOnSegment(actorPosition, Origin, BeamEnd);
        float distanceSquared = Vector2.DistanceSquared(actorPosition, closestPoint);
        return distanceSquared <= actorRadius * actorRadius;
    }

    private bool DidActorPassThroughBeam(Vector2? previousPosition, Vector2 currentPosition, float actorRadius)
    {
        if (previousPosition is not Vector2 stepStart || actorRadius <= 0f)
        {
            return false;
        }

        float stepDistanceSquared = Vector2.DistanceSquared(stepStart, currentPosition);
        float maxStepSquared = MaxContinuousStepDistance * MaxContinuousStepDistance;
        if (stepDistanceSquared <= SegmentLengthEpsilon || stepDistanceSquared > maxStepSquared)
        {
            return false;
        }

        Vector2 stepDirection = Vector2.Normalize(currentPosition - stepStart);
        float stepDistance = MathF.Sqrt(stepDistanceSquared);
        return VisionRaycaster.TryIntersectRayWithSegment(stepStart, stepDirection, Origin, BeamEnd, out float hitDistance)
            && hitDistance <= stepDistance;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float lengthSquared = segment.LengthSquared();

        if (lengthSquared <= SegmentLengthEpsilon)
        {
            return segmentStart;
        }

        float projection = Vector2.Dot(point - segmentStart, segment) / lengthSquared;
        float clampedProjection = Math.Clamp(projection, 0f, 1f);
        return segmentStart + (segment * clampedProjection);
    }
}
