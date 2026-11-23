using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Wall-mounted 3D security camera with a sweeping VisionRaycaster cone that alerts guards unless disabled by FuseBox or EMP (Ticket #29 — Hatem).
/// </summary>
public sealed class SecurityCamera
{
    public const string DefaultPowerGroupId = "lobby_lights";
    public const string AllPowerGroupId = "all";
    public const float DefaultSweepAmplitudeDegrees = 55f;
    public const float DefaultSweepSpeedRadians = 0.7f;
    public const float DefaultVisionRange = 225f;
    public const float DefaultHalfAngleDegrees = 26f;

    private const float FullConeFovScale = 2f;
    private const float AlarmRetriggerCooldownSeconds = 3f;
    private const float MinVisibleDistanceSquared = 1f;

    private float _sweepElapsedSeconds;
    private float _alarmCooldownRemaining;

    public string Id { get; }
    public Vector2 Position { get; }
    public string PowerGroupId { get; }
    public float CenterAngleDegrees { get; }
    public float SweepAmplitudeDegrees { get; }
    public float SweepSpeedRadians { get; }
    public float VisionRange { get; }
    public float HalfAngleDegrees { get; }
    public float CurrentAngleDegrees { get; private set; }
    public bool IsPowered { get; private set; } = true;
    public float EmpDisabledRemainingSeconds { get; private set; }
    public DetectionMeter Detection { get; } = new();
    public float DetectionProgress => Detection.Progress;
    public bool IsActive => IsPowered && EmpDisabledRemainingSeconds <= 0f;

    public SecurityCamera(
        string id,
        Vector2 position,
        float centerAngleDegrees,
        float sweepAmplitudeDegrees = DefaultSweepAmplitudeDegrees,
        float sweepSpeedRadians = DefaultSweepSpeedRadians,
        float visionRange = DefaultVisionRange,
        float halfAngleDegrees = DefaultHalfAngleDegrees,
        string powerGroupId = DefaultPowerGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(powerGroupId);

        Id = id;
        Position = position;
        CenterAngleDegrees = centerAngleDegrees;
        CurrentAngleDegrees = centerAngleDegrees;
        SweepAmplitudeDegrees = sweepAmplitudeDegrees;
        SweepSpeedRadians = sweepSpeedRadians;
        VisionRange = visionRange;
        HalfAngleDegrees = halfAngleDegrees;
        PowerGroupId = powerGroupId;
    }

    public void ApplyPowerState(PowerStateChangedEvent powerEvent)
    {
        if (!MatchesPowerGroup(powerEvent.TargetGroupId)) return;

        IsPowered = powerEvent.IsPowered;
        if (!IsPowered) Detection.Reset();
    }

    public void DisableTemporarily(float durationSeconds)
    {
        if (durationSeconds <= 0f) return;

        EmpDisabledRemainingSeconds = MathF.Max(EmpDisabledRemainingSeconds, durationSeconds);
        Detection.Reset();
    }

    public bool Update(float deltaTime, GameContext context, IReadOnlyList<RectangleBounds>? visionBlockers = null)
    {
        if (deltaTime <= 0f) return false;

        UpdateTimers(deltaTime);
        SyncWithContextPower(context);

        if (!IsActive)
        {
            Detection.Reset();
            return false;
        }

        _sweepElapsedSeconds += deltaTime;
        CurrentAngleDegrees = CenterAngleDegrees + (MathF.Sin(_sweepElapsedSeconds * SweepSpeedRadians) * SweepAmplitudeDegrees);

        IReadOnlyList<RectangleBounds> blockers = visionBlockers ?? context.Walls;
        bool playerInCone = IsPlayerInsideCone(context.PlayerPosition, blockers);
        float distance = Vector2.Distance(Position, context.PlayerPosition);

        bool fullyDetected = Detection.Update(
            deltaTime,
            isVisibleInCone: playerInCone,
            isInProximityCircle: false,
            distanceToPlayer: distance,
            activeVisionRange: VisionRange,
            isPlayerSneaking: context.IsPlayerSneaking);

        if (!fullyDetected || _alarmCooldownRemaining > 0f) return false;

        _alarmCooldownRemaining = AlarmRetriggerCooldownSeconds;
        context.IsAlarmTriggered = true;
        context.EventBus.Publish(new AlarmTriggeredEvent(context.PlayerPosition, $"SecurityCamera:{Id}"));
        return true;
    }

    public IReadOnlyList<Vector2> ComputeVisionCone(IReadOnlyList<RectangleBounds> visionBlockers) =>
        !IsActive
            ? Array.Empty<Vector2>()
            : VisionRaycaster.ComputeVisionCone(
                Position,
                CurrentAngleDegrees,
                visionBlockers,
                fovDegrees: HalfAngleDegrees * FullConeFovScale,
                maxDistance: VisionRange,
                rayCount: Viewport3DConfig.VisionRayCount);

    public bool IsPlayerInsideCone(Vector2 playerPosition, IReadOnlyList<RectangleBounds> visionBlockers)
    {
        if (!IsActive) return false;

        Vector2 toPlayer = playerPosition - Position;
        float distSq = toPlayer.LengthSquared();
        if (distSq > VisionRange * VisionRange) return false;
        if (distSq < MinVisibleDistanceSquared) return true;

        float rad = CurrentAngleDegrees * HeistConstants.DegToRad;
        Vector2 forward = new(MathF.Cos(rad), MathF.Sin(rad));
        float dot = Vector2.Dot(forward, Vector2.Normalize(toPlayer));
        float cosHalf = MathF.Cos(HalfAngleDegrees * HeistConstants.DegToRad);
        if (dot < cosHalf) return false;

        return visionBlockers.Count == 0 ||
               CollisionHelper.HasLineOfSight(Position, playerPosition, visionBlockers);
    }

    private void UpdateTimers(float deltaTime)
    {
        if (EmpDisabledRemainingSeconds > 0f)
            EmpDisabledRemainingSeconds = MathF.Max(0f, EmpDisabledRemainingSeconds - deltaTime);
        if (_alarmCooldownRemaining > 0f)
            _alarmCooldownRemaining = MathF.Max(0f, _alarmCooldownRemaining - deltaTime);
    }

    private void SyncWithContextPower(GameContext context)
    {
        if (string.Equals(PowerGroupId, DefaultPowerGroupId, StringComparison.OrdinalIgnoreCase) && !context.AreLobbyLightsPowered)
            IsPowered = false;
    }

    private bool MatchesPowerGroup(string targetGroupId) =>
        string.Equals(targetGroupId, Id, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(targetGroupId, PowerGroupId, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(targetGroupId, AllPowerGroupId, StringComparison.OrdinalIgnoreCase);
}
