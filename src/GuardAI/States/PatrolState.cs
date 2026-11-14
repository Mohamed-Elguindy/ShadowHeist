using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.Pathfinding;

namespace ShadowHeist.GuardAI.States;

/// <summary>
/// Patrol behavior: walks sequentially between designated waypoints, pausing briefly at each stop.
/// </summary>
public sealed class PatrolState : IGuardState
{
    private const float DefaultWaitDuration = 1.5f;

    private readonly Guard _guard;
    private readonly IReadOnlyList<Vector2> _waypoints;
    private readonly float _waitDuration;

    private IReadOnlyList<Vector2> _activePath = Array.Empty<Vector2>();
    private int _activePathIndex;
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
        _activePath = Array.Empty<Vector2>();
        _activePathIndex = 0;
    }

    public IGuardState Update(float deltaTime, GameContext context)
    {
        if (_isWaiting)
        {
            UpdateWaiting(deltaTime);
            return this;
        }

        UpdatePatrolMovement(deltaTime, context.Walls);
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
            _activePath = Array.Empty<Vector2>();
            _activePathIndex = 0;
        }
    }

    private void UpdatePatrolMovement(float deltaTime, IReadOnlyList<RectangleBounds> walls)
    {
        if (_activePath.Count == 0)
        {
            var computed = AStarPathfinder.FindPath(_guard.Position, TargetWaypoint, walls);
            _activePath = computed.Count > 0 ? computed : new List<Vector2> { TargetWaypoint };
            _activePathIndex = 0;
        }

        if (_guard.StepAlongPath(_activePath, ref _activePathIndex, deltaTime))
        {
            _guard.Position = TargetWaypoint;
            _isWaiting = true;
            _waitTimer = _waitDuration;
        }
    }
}
