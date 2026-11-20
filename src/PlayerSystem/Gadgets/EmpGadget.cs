using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem.Gadgets;

/// <summary>
/// Thrown EMP grenade gadget that lands along the aim trajectory and temporarily disables powered devices across any map.
/// </summary>
public sealed class EmpGadget : IGadget
{
    public const int DefaultCharges = 2;
    public const float DefaultMaxThrowDistance = 240f;
    public const float DefaultEmpRadius = 220f;
    public const float DefaultDisableDurationSeconds = 6f;

    private const string AllPowerGroupId = "all";
    private const float GrenadeCollisionRadius = 10f;

    private readonly Dictionary<string, bool> _externalPowerStates = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<RectangleBounds>? _walls;
    private bool _isEmittingPowerEvent;

    public string Name => "EMP";
    public int RemainingCharges { get; private set; }
    public float MaxThrowDistance { get; }
    public float EmpRadius { get; }
    public float DisableDurationSeconds { get; }
    public float ActiveRemainingSeconds { get; private set; }
    public bool IsActivePulse => ActiveRemainingSeconds > 0f;
    public Vector2 LastThrowOrigin { get; private set; }
    public Vector2 LastLandingPoint { get; private set; }

    public EmpGadget(
        int initialCharges = DefaultCharges,
        float maxThrowDistance = DefaultMaxThrowDistance,
        float empRadius = DefaultEmpRadius,
        float disableDurationSeconds = DefaultDisableDurationSeconds)
    {
        RemainingCharges = Math.Max(0, initialCharges);
        MaxThrowDistance = MathF.Max(0f, maxThrowDistance);
        EmpRadius = MathF.Max(0f, empRadius);
        DisableDurationSeconds = MathF.Max(0f, disableDurationSeconds);
    }

    public void SetWalls(IReadOnlyList<RectangleBounds>? walls) => _walls = walls;

    public void ObserveExternalPowerEvent(PowerStateChangedEvent evt)
    {
        if (_isEmittingPowerEvent || string.IsNullOrWhiteSpace(evt.TargetGroupId))
        {
            return;
        }

        if (string.Equals(evt.TargetGroupId, AllPowerGroupId, StringComparison.OrdinalIgnoreCase))
        {
            foreach (string key in _externalPowerStates.Keys.ToList())
            {
                _externalPowerStates[key] = evt.IsPowered;
            }
        }

        _externalPowerStates[evt.TargetGroupId] = evt.IsPowered;
    }

    public bool TryUse(Vector2 origin, Vector2 target, GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        if (RemainingCharges <= 0 || IsActivePulse)
        {
            return false;
        }

        RemainingCharges--;
        LastThrowOrigin = origin;
        LastLandingPoint = GadgetThrowHelper.ResolveLandingPoint(
            origin,
            target,
            MaxThrowDistance,
            GrenadeCollisionRadius,
            _walls);
        ActiveRemainingSeconds = DisableDurationSeconds;
        PublishManagedPowerEvent(eventBus, AllPowerGroupId, isPowered: false);
        return true;
    }

    public void Update(float deltaTime, GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        if (ActiveRemainingSeconds <= 0f || deltaTime <= 0f)
        {
            return;
        }

        ActiveRemainingSeconds = MathF.Max(0f, ActiveRemainingSeconds - deltaTime);
        if (ActiveRemainingSeconds <= 0f)
        {
            RestoreNonOverriddenPowerStates(eventBus);
        }
    }

    public void ClearExternalStates()
    {
        ActiveRemainingSeconds = 0f;
        _externalPowerStates.Clear();
    }

    private void RestoreNonOverriddenPowerStates(GameEventBus eventBus)
    {
        if (_externalPowerStates.TryGetValue(AllPowerGroupId, out bool allExternalState) && !allExternalState)
        {
            return;
        }

        PublishManagedPowerEvent(eventBus, AllPowerGroupId, isPowered: true);
        foreach ((string groupId, bool isPowered) in _externalPowerStates)
        {
            if (!isPowered && !string.Equals(groupId, AllPowerGroupId, StringComparison.OrdinalIgnoreCase))
            {
                PublishManagedPowerEvent(eventBus, groupId, isPowered: false);
            }
        }
    }

    private void PublishManagedPowerEvent(GameEventBus eventBus, string targetGroupId, bool isPowered)
    {
        _isEmittingPowerEvent = true;
        try
        {
            eventBus.Publish(new PowerStateChangedEvent(targetGroupId, isPowered));
        }
        finally
        {
            _isEmittingPowerEvent = false;
        }
    }
}
