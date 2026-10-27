using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.GuardAI.States;

/// <summary>
/// Patrol behavior: walks sequentially between designated waypoints, pausing briefly at each stop.
/// </summary>
public sealed class PatrolState : IGuardState
{
    private const float WaypointReachThreshold = 6f;
    private const float DefaultWaitDuration = 1.5f;
    private const float TurnRate = 360f;

    private readonly Guard _guard;
    private readonly IReadOnlyList<Vector2> _waypoints;
    private readonly float _waitDuration;

    private int _currentWaypointIndex;
    private float _waitTimer;
    private bool _isWaiting;

    public string StateName => "Patrol";
    public int CurrentWaypointIndex => _currentWaypointIndex;
    public Vector2 TargetWaypoint => _waypoints[_currentWaypointIndex];
    public bool IsWaiting => _isWaiting;
    public float WaitTimer => _waitTimer;

    public PatrolState(Guard guard, IReadOnlyList<Vector2> waypoints, float waitDuration = DefaultWaitDuration)
    {
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(waypoints);

        if (waypoints.Count == 0)
        {
            throw new ArgumentException("Patrol waypoints cannot be empty.", nameof(waypoints));
        }

        _guard = guard;
        _waypoints = waypoints;
        _waitDuration = waitDuration;
    }

    public void Enter()
    {
        _isWaiting = false;
        _waitTimer = 0f;
    }

    public IGuardState Update(float deltaTime, GameContext context)
    {
        if (_isWaiting)
        {
            UpdateWaiting(deltaTime);
            return this;
        }

        UpdatePatrolMovement(deltaTime);
        return this;
    }

    private void UpdateWaiting(float deltaTime)
    {
        _waitTimer -= deltaTime;
        if (_waitTimer <= 0f)
        {
            _waitTimer = 0f;
            _isWaiting = false;
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
        }
    }

    private void UpdatePatrolMovement(float deltaTime)
    {
        var toTarget = TargetWaypoint - _guard.Position;
        float distance = toTarget.Length();
        float step = _guard.MoveSpeed * deltaTime;

        if (distance <= WaypointReachThreshold || step >= distance)
        {
            _guard.Position = TargetWaypoint;
            _isWaiting = true;
            _waitTimer = _waitDuration;
            return;
        }

        var direction = Vector2.Normalize(toTarget);
        _guard.RotateToward(direction, deltaTime, TurnRate);
        _guard.Position += direction * step;
    }
}
