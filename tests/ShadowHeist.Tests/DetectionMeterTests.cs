using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Unit tests for progressive Guard DetectionMeter (0% -> 100%), close-proximity awareness circle, and alarm vision expansion (Ticket #22 — Hatem).
/// </summary>
public sealed class DetectionMeterTests
{
    [Fact]
    public void DetectionMeter_FillsFasterAtCloseDistance_AndSlowerWhenSneaking()
    {
        float closeStandRate = DetectionMeter.ComputeFillRate(
            isVisibleInCone: true,
            isInProximityCircle: false,
            distanceToPlayer: 40f,
            activeVisionRange: 235f,
            isPlayerSneaking: false);

        float farStandRate = DetectionMeter.ComputeFillRate(
            isVisibleInCone: true,
            isInProximityCircle: false,
            distanceToPlayer: 210f,
            activeVisionRange: 235f,
            isPlayerSneaking: false);

        float closeSneakRate = DetectionMeter.ComputeFillRate(
            isVisibleInCone: true,
            isInProximityCircle: false,
            distanceToPlayer: 40f,
            activeVisionRange: 235f,
            isPlayerSneaking: true);

        Assert.True(closeStandRate > farStandRate);
        Assert.True(closeSneakRate < closeStandRate);
    }

    [Fact]
    public void DetectionMeter_DecaysTowardsZero_WhenPlayerBreaksLineOfSight()
    {
        var meter = new DetectionMeter();
        meter.Update(0.2f, isVisibleInCone: true, isInProximityCircle: false, distanceToPlayer: 100f, activeVisionRange: 235f, isPlayerSneaking: false);
        float initialProgress = meter.Progress;
        Assert.True(initialProgress > 0f);

        meter.Update(0.2f, isVisibleInCone: false, isInProximityCircle: false, distanceToPlayer: 100f, activeVisionRange: 235f, isPlayerSneaking: false);
        Assert.True(meter.Progress < initialProgress);
    }

    [Fact]
    public void Guard_DetectsPlayerInCloseProximityCircle_EvenBehindGuard()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(65f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), new[] { new Vector2(400f, 100f) })
        {
            RotationDegrees = 0f
        };

        Assert.True(GuardSensorHelper.IsPlayerInProximityCircle(guard, context));
        guard.Update(0.15f, context);
        Assert.True(guard.DetectionProgress > 0f);
    }

    [Fact]
    public void GuardSensorHelper_ExpandsVisionRangeAndAngle_WhenAlarmIsTriggered()
    {
        Assert.True(GuardSensorHelper.ResolveVisionRange(true) > GuardSensorHelper.ResolveVisionRange(false));
        Assert.True(GuardSensorHelper.ResolveHalfAngleDegrees(true) > GuardSensorHelper.ResolveHalfAngleDegrees(false));
    }
}
