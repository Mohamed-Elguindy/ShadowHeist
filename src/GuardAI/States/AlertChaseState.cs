using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.GuardAI.States;

/// <summary>
/// Guard triggers the heist alarm and pursues the spotted intruder.
/// </summary>
public sealed class AlertChaseState : IGuardState
{
    private const float ChaseSpeed = 115f;
    private const float CatchRadius = 28f;
    private const float SearchTimeoutSeconds = 5f;
    private const float TurnRate = 540f;

    private readonly Guard _guard;
    private readonly IReadOnlyList<Vector2> _patrolWaypoints;
    private Vector2 _lastSeenPos;
    private float _lostSightTimer;

    public string StateName => "Alert";

    public AlertChaseState(Guard guard, IReadOnlyList<Vector2> patrolWaypoints, Vector2 spottedPosition)
    {
        _guard = guard;
        _patrolWaypoints = patrolWaypoints;
        _lastSeenPos = spottedPosition;
    }

    public void Enter() => _lostSightTimer = SearchTimeoutSeconds;

    public IGuardState Update(float deltaTime, GameContext context)
    {
        if (!context.IsAlarmTriggered)
        {
            context.IsAlarmTriggered = true;
            context.EventBus.Publish(new AlarmTriggeredEvent(context.PlayerPosition, $"SpottedBy:{_guard.Id}"));
        }

        if (GuardSensorHelper.CanSeePlayer(_guard, context, GuardSensorHelper.DefaultVisionRange * 1.25f, 55f))
        {
            _lastSeenPos = context.PlayerPosition;
            _lostSightTimer = SearchTimeoutSeconds;
        }
        else
        {
            _lostSightTimer -= deltaTime;
        }

        if (Vector2.Distance(_guard.Position, context.PlayerPosition) <= CatchRadius)
        {
            context.Outcome = HeistOutcome.Caught;
            return this;
        }

        Vector2 toTarget = _lastSeenPos - _guard.Position;
        float dist = toTarget.Length();
        if (dist > 4f)
        {
            Vector2 dir = Vector2.Normalize(toTarget);
            _guard.RotateToward(dir, deltaTime, TurnRate);
            GuardSensorHelper.MoveWithCollision(_guard, dir * MathF.Min(ChaseSpeed * deltaTime, dist), context.Walls);
        }

        return _lostSightTimer <= 0f ? new PatrolState(_guard, _patrolWaypoints) : this;
    }
}
