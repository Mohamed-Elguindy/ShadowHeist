using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.States;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Represents a security guard entity executing an IGuardState finite state machine.
/// </summary>
public sealed class Guard
{
    private const float DefaultMoveSpeed = 80f;
    private const float DefaultTurnRate = 360f;

    public string Id { get; }
    public Vector2 Position { get; set; }
    public float RotationDegrees { get; set; }
    public float MoveSpeed { get; set; }
    public IReadOnlyList<Vector2> Waypoints { get; }
    public IGuardState CurrentState { get; private set; }

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

    public void Update(float deltaTime, GameContext context)
    {
        var nextState = CurrentState.Update(deltaTime, context);
        if (!ReferenceEquals(nextState, CurrentState))
        {
            TransitionTo(nextState);
        }
    }

    public void RotateToward(Vector2 direction, float deltaTime, float turnRate = DefaultTurnRate)
    {
        if (direction.LengthSquared() < 0.0001f)
        {
            return;
        }

        float targetAngle = MathF.Atan2(direction.Y, direction.X) * (180f / MathF.PI);
        RotationDegrees = MoveTowardsAngle(RotationDegrees, targetAngle, turnRate * deltaTime);
    }

    public static float MoveTowardsAngle(float current, float target, float maxDelta)
    {
        float diff = (target - current) % 360f;
        if (diff > 180f) diff -= 360f;
        if (diff < -180f) diff += 360f;

        if (MathF.Abs(diff) <= maxDelta)
        {
            return target;
        }

        return current + MathF.Sign(diff) * maxDelta;
    }
}
