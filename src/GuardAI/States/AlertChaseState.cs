using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.Pathfinding;

namespace ShadowHeist.GuardAI.States;

/// <summary>
/// Guard triggers the heist alarm and pursues the spotted intruder via A* pathfinding.
/// </summary>
public sealed class AlertChaseState : IGuardPathState
{
    private const float ChaseSpeed = 115f;
    private const float CatchRadius = 28f;
    private const float SearchTimeoutSeconds = 5f;
    private const float AlertVisionMultiplier = 1.25f;
    private const float AlertHalfAngleDegrees = 55f;
    private const float RepathDistanceSquared = 64f;

    private readonly Guard _guard;
    private readonly IReadOnlyList<Vector2> _patrolWaypoints;

    private IReadOnlyList<Vector2> _pathWaypoints;
    private Vector2 _lastSeenPosition;
    private Vector2 _lastPathGoal;
    private int _currentPathIndex;
    private float _lostSightTimer;
    private bool _pathComputedWithWalls;

    public string StateName => "Alert";
    public Vector2 TargetPosition => _lastSeenPosition;
    public IReadOnlyList<Vector2> PathWaypoints => _pathWaypoints;
    public int CurrentPathIndex => _currentPathIndex;

    public AlertChaseState(Guard guard, IReadOnlyList<Vector2> patrolWaypoints, Vector2 spottedPosition)
    {
        _guard = guard;
        _patrolWaypoints = patrolWaypoints;
        _lastSeenPosition = spottedPosition;
        _lastPathGoal = spottedPosition;
        _pathWaypoints = new List<Vector2> { spottedPosition };
    }

    public void Enter()
    {
        _lostSightTimer = SearchTimeoutSeconds;
        _currentPathIndex = 0;
        _pathComputedWithWalls = false;
    }

    public IGuardState Update(float deltaTime, GameContext context)
    {
        EnsureAlarmActive(context);
        TrackSpottedIntruder(deltaTime, context);

        if (Vector2.Distance(_guard.Position, context.PlayerPosition) <= CatchRadius)
        {
            context.Outcome = HeistOutcome.Caught;
            return this;
        }

        if (!_pathComputedWithWalls && context.Walls.Count > 0)
        {
            RebuildChasePath(context.Walls);
        }

        float speedMultiplier = ChaseSpeed / MathF.Max(1f, _guard.MoveSpeed);
        _guard.StepAlongPath(_pathWaypoints, ref _currentPathIndex, deltaTime, speedMultiplier);

        return _lostSightTimer <= 0f ? new PatrolState(_guard, _patrolWaypoints) : this;
    }

    private void EnsureAlarmActive(GameContext context)
    {
        if (context.IsAlarmTriggered)
        {
            return;
        }

        context.IsAlarmTriggered = true;
        context.EventBus.Publish(new AlarmTriggeredEvent(context.PlayerPosition, $"SpottedBy:{_guard.Id}"));
    }

    private void TrackSpottedIntruder(float deltaTime, GameContext context)
    {
        bool playerVisible = GuardSensorHelper.CanSeePlayer(
            _guard,
            context,
            GuardSensorHelper.DefaultVisionRange * AlertVisionMultiplier,
            AlertHalfAngleDegrees);

        if (!playerVisible)
        {
            _lostSightTimer -= deltaTime;
            return;
        }

        _lastSeenPosition = context.PlayerPosition;
        _lostSightTimer = SearchTimeoutSeconds;

        if (!_pathComputedWithWalls || Vector2.DistanceSquared(_lastPathGoal, _lastSeenPosition) > RepathDistanceSquared)
        {
            RebuildChasePath(context.Walls);
        }
    }

    private void RebuildChasePath(IReadOnlyList<RectangleBounds> walls)
    {
        _currentPathIndex = 0;
        _lastPathGoal = _lastSeenPosition;

        if (walls.Count > 0)
        {
            _pathComputedWithWalls = true;
            IReadOnlyList<Vector2> computed = AStarPathfinder.FindPath(_guard.Position, _lastSeenPosition, walls);
            _pathWaypoints = computed.Count > 0 ? computed : new List<Vector2> { _lastSeenPosition };
            return;
        }

        _pathComputedWithWalls = false;
        _pathWaypoints = new List<Vector2> { _lastSeenPosition };
    }
}
