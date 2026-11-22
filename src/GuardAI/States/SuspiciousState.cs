using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.Pathfinding;

namespace ShadowHeist.GuardAI.States;

/// <summary>
/// Guard navigates via A* to the last heard sound origin, scans the surroundings, and resumes its prior state.
/// </summary>
public sealed class SuspiciousState : IGuardPathState
{
    private const float DefaultInvestigateDuration = 2.5f;
    private const float SuspiciousSpeedMultiplier = 1.15f;
    private const float ScanTurnSpeed = 90f;
    private const float ScanFrequencyRadians = 4f;
    private const float FullCircleDegrees = 360f;

    private readonly Guard _guard;
    private readonly IGuardState _returnState;
    private readonly float _investigateDuration;

    private IReadOnlyList<Vector2> _pathWaypoints = Array.Empty<Vector2>();
    private int _currentPathIndex;
    private float _investigateTimer;
    private bool _isInvestigating;
    private bool _pathComputedWithWalls;

    public string StateName => "Suspicious";
    public Vector2 InvestigationTarget { get; private set; }
    public Vector2 TargetPosition => InvestigationTarget;
    public IReadOnlyList<Vector2> PathWaypoints => _pathWaypoints;
    public int CurrentPathIndex => _currentPathIndex;
    public bool IsInvestigating => _isInvestigating;
    public float InvestigateTimer => _investigateTimer;

    public SuspiciousState(
        Guard guard,
        IReadOnlyList<Vector2> patrolWaypoints,
        Vector2 investigationTarget)
        : this(guard, investigationTarget, new PatrolState(guard, patrolWaypoints))
    {
    }

    public SuspiciousState(
        Guard guard,
        Vector2 investigationTarget,
        IGuardState returnState,
        IReadOnlyList<RectangleBounds>? walls = null,
        float investigateDuration = DefaultInvestigateDuration)
    {
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(returnState);

        _guard = guard;
        _returnState = returnState;
        _investigateDuration = investigateDuration;
        InvestigationTarget = investigationTarget;

        RebuildPath(walls);
    }

    public void Enter()
    {
        _isInvestigating = false;
        _investigateTimer = _investigateDuration;
    }

    public void UpdateInvestigationTarget(Vector2 newTarget, IReadOnlyList<RectangleBounds>? walls = null)
    {
        InvestigationTarget = newTarget;
        _isInvestigating = false;
        _investigateTimer = _investigateDuration;
        RebuildPath(walls);
    }

    public IGuardState Update(float deltaTime, GameContext context)
    {
        if (GuardSensorHelper.TrySpotIntruder(_guard, _guard.Waypoints, context, deltaTime, out IGuardState alertState))
        {
            return alertState;
        }

        if (!_pathComputedWithWalls && context.Walls.Count > 0)
        {
            RebuildPath(context.Walls);
        }

        if (_isInvestigating)
        {
            return UpdateInvestigationScan(deltaTime);
        }

        if (_guard.StepAlongPath(_pathWaypoints, ref _currentPathIndex, deltaTime, SuspiciousSpeedMultiplier))
        {
            if (_pathWaypoints.Count > 0)
            {
                _guard.Position = _pathWaypoints[^1];
            }

            _isInvestigating = true;
            _investigateTimer = _investigateDuration;
        }

        return this;
    }

    private void RebuildPath(IReadOnlyList<RectangleBounds>? walls)
    {
        _currentPathIndex = 0;

        if (walls is { Count: > 0 })
        {
            _pathComputedWithWalls = true;
            _pathWaypoints = AStarPathfinder.FindPath(_guard.Position, InvestigationTarget, walls);
            return;
        }

        _pathComputedWithWalls = false;
        _pathWaypoints = new List<Vector2> { InvestigationTarget };
    }

    private IGuardState UpdateInvestigationScan(float deltaTime)
    {
        _investigateTimer -= deltaTime;
        float scanWave = MathF.Sin((_investigateDuration - _investigateTimer) * ScanFrequencyRadians);
        _guard.RotationDegrees = (_guard.RotationDegrees + (scanWave * ScanTurnSpeed * deltaTime) + FullCircleDegrees) % FullCircleDegrees;

        if (_investigateTimer <= 0f)
        {
            _investigateTimer = 0f;
            return _returnState;
        }

        return this;
    }
}
