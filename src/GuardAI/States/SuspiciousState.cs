using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.GuardAI.States;

/// <summary>
/// Guard investigates the last heard sound origin before returning to patrol.
/// </summary>
public sealed class SuspiciousState : IGuardState
{
    private const float ArrivalRadius = 18f;
    private const float InvestigateDuration = 2.5f;
    private const float TurnRate = 270f;

    private readonly Guard _guard;
    private readonly IReadOnlyList<Vector2> _patrolWaypoints;
    private Vector2 _targetPoint;
    private float _investigateTimer = InvestigateDuration;
    private float _elapsed;

    public string StateName => "Suspicious";

    public SuspiciousState(Guard guard, IReadOnlyList<Vector2> patrolWaypoints, Vector2 noiseOrigin)
    {
        _guard = guard;
        _patrolWaypoints = patrolWaypoints;
        _targetPoint = noiseOrigin;
    }

    public void Enter() => _investigateTimer = InvestigateDuration;

    public IGuardState Update(float deltaTime, GameContext context)
    {
        _elapsed += deltaTime;
        if (GuardSensorHelper.TrySpotIntruder(_guard, _patrolWaypoints, context, out IGuardState alertState))
        {
            return alertState;
        }

        if (_guard.ConsumeHeardNoise() is Vector2 fresherNoise)
        {
            _targetPoint = fresherNoise;
            _investigateTimer = InvestigateDuration;
        }

        Vector2 toNoise = _targetPoint - _guard.Position;
        float distance = toNoise.Length();
        if (distance > ArrivalRadius)
        {
            Vector2 dir = Vector2.Normalize(toNoise);
            _guard.RotateToward(dir, deltaTime, TurnRate);
            GuardSensorHelper.MoveWithCollision(_guard, dir * MathF.Min(_guard.MoveSpeed * deltaTime, distance), context.Walls);
            return this;
        }

        _guard.RotationDegrees += MathF.Sin(_elapsed * 2.5f) * 60f * deltaTime;
        _investigateTimer -= deltaTime;
        return _investigateTimer <= 0f ? new PatrolState(_guard, _patrolWaypoints) : this;
    }
}
