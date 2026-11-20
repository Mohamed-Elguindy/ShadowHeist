using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.Pathfinding;
using ShadowHeist.GuardAI.States;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Represents a security guard entity executing an IGuardState finite state machine.
/// </summary>
public sealed class Guard
{
    private const float DefaultMoveSpeed = 92f;
    private const float DefaultTurnRate = 360f;
    private const float WaypointReachThreshold = 6f;
    private const float DirectionEpsilonSquared = 0.0001f;
    private const float FullCircleDegrees = 360f;
    private const float HalfCircleDegrees = 180f;

    public string Id { get; }
    public Vector2 Position { get; set; }
    public float RotationDegrees { get; set; }
    public float MoveSpeed { get; set; }
    public IReadOnlyList<Vector2> Waypoints { get; }
    public IGuardState CurrentState { get; private set; }
    public DetectionMeter Detection { get; } = new();
    public float DetectionProgress => Detection.Progress;

    public Guard(string id, Vector2 initialPosition, IReadOnlyList<Vector2> waypoints, float moveSpeed = DefaultMoveSpeed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(waypoints);

        if (waypoints.Count == 0)
        {
            throw new ArgumentException("Guard must have at least one patrol waypoint.", nameof(waypoints));
        }

        Id = id;
        Position = initialPosition;
        Waypoints = waypoints;
        MoveSpeed = moveSpeed;

        var initialPatrolState = new PatrolState(this, waypoints);
        CurrentState = initialPatrolState;
        initialPatrolState.Enter();
    }

    public void TransitionTo(IGuardState newState)
    {
        ArgumentNullException.ThrowIfNull(newState);

        if (ReferenceEquals(CurrentState, newState))
        {
            return;
        }

        CurrentState = newState;
        CurrentState.Enter();
    }

    public bool OnNoiseEmitted(NoiseEmittedEvent noiseEvent, IReadOnlyList<RectangleBounds>? walls = null)
    {
        if (noiseEvent.Radius <= 0f)
        {
            return false;
        }

        float distanceSquared = Vector2.DistanceSquared(Position, noiseEvent.Origin);
        if (distanceSquared > noiseEvent.Radius * noiseEvent.Radius || !CanReachOrigin(noiseEvent.Origin, walls))
        {
            return false;
        }

        NotifyReachableNoise(noiseEvent.Origin, walls);
        return true;
    }

    public void NotifyNoise(Vector2 origin, IReadOnlyList<RectangleBounds>? walls = null)
    {
        if (CanReachOrigin(origin, walls))
        {
            NotifyReachableNoise(origin, walls);
        }
    }

    private static bool CanReachOrigin(Vector2 guardPos, Vector2 origin, IReadOnlyList<RectangleBounds>? walls) =>
        walls is not { Count: > 0 } || AStarPathfinder.FindPath(guardPos, origin, walls).Count > 0;

    private bool CanReachOrigin(Vector2 origin, IReadOnlyList<RectangleBounds>? walls) =>
        CanReachOrigin(Position, origin, walls);

    private void NotifyReachableNoise(Vector2 origin, IReadOnlyList<RectangleBounds>? walls)
    {
        if (CurrentState is AlertChaseState)
        {
            return;
        }

        if (CurrentState is SuspiciousState activeSuspicious)
        {
            activeSuspicious.UpdateInvestigationTarget(origin, walls);
            return;
        }

        TransitionTo(new SuspiciousState(this, origin, CurrentState, walls));
    }

    public void Update(float deltaTime, GameContext context)
    {
        IGuardState nextState = CurrentState.Update(deltaTime, context);
        if (context.Walls.Count > 0)
        {
            Position = CollisionHelper.ResolveWallCollision(Position, CollisionHelper.DefaultActorRadius, context.Walls);
        }

        if (!ReferenceEquals(nextState, CurrentState))
        {
            TransitionTo(nextState);
        }
    }

    public bool StepAlongPath(IReadOnlyList<Vector2> path, ref int currentIndex, float deltaTime, float speedMultiplier = 1f)
    {
        if (currentIndex >= path.Count)
        {
            return true;
        }

        Vector2 targetNode = path[currentIndex];
        Vector2 toTarget = targetNode - Position;
        float distance = toTarget.Length();
        float step = MoveSpeed * speedMultiplier * deltaTime;

        if (distance <= WaypointReachThreshold || step >= distance)
        {
            Position = targetNode;
            currentIndex++;
            return currentIndex >= path.Count;
        }

        Vector2 direction = Vector2.Normalize(toTarget);
        RotateToward(direction, deltaTime);
        Position += direction * step;
        return false;
    }

    public void RotateToward(Vector2 direction, float deltaTime, float turnRate = DefaultTurnRate)
    {
        if (direction.LengthSquared() < DirectionEpsilonSquared)
        {
            return;
        }

        float targetAngle = MathF.Atan2(direction.Y, direction.X) * HeistConstants.RadToDeg;
        RotationDegrees = MoveTowardsAngle(RotationDegrees, targetAngle, turnRate * deltaTime);
    }

    public static float MoveTowardsAngle(float current, float target, float maxDelta)
    {
        float diff = (target - current) % FullCircleDegrees;
        if (diff > HalfCircleDegrees) diff -= FullCircleDegrees;
        if (diff < -HalfCircleDegrees) diff += FullCircleDegrees;

        if (MathF.Abs(diff) <= maxDelta)
        {
            return target;
        }

        return current + (MathF.Sign(diff) * maxDelta);
    }
}
