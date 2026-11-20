using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Rendering3D;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class TacticalCameraControllerTests
{
    private static readonly Vector2 PlayerSpawn = new(200f, 360f);

    [Fact]
    public void Reset_SetsFollowFocusAndClearsPeekAndShake()
    {
        var controller = new TacticalCameraController();
        controller.SetTacticalPeek(true, PlayerSpawn, PlayerSpawn + new Vector2(300f, 0f));
        controller.TriggerAlarmShake(1f);
        controller.Update(0.016f, PlayerSpawn);

        controller.Reset(PlayerSpawn);

        Assert.Equal(Vector3.Zero, controller.CurrentPeekOffset);
        Assert.Equal(Vector3.Zero, controller.CurrentShakeOffset);
        Assert.Equal(0f, controller.ShakeTrauma);
        Assert.Equal(controller.FocusPoint, controller.Camera.Target);
    }

    [Fact]
    public void ToggleOverviewMode_SwitchesBetweenFollowAndOverviewFov()
    {
        var controller = new TacticalCameraController();
        controller.Reset(PlayerSpawn);
        Assert.Equal(Viewport3DConfig.FollowFovDegrees, controller.Camera.FovY);

        controller.ToggleOverviewMode();
        controller.Update(0.016f, PlayerSpawn);

        Assert.True(controller.IsOverviewMode);
        Assert.Equal(Viewport3DConfig.OverviewFovDegrees, controller.Camera.FovY);
    }

    [Fact]
    public void ApplyZoomWheel_ClampsBetweenMinZoomAndMaxZoom()
    {
        var controller = new TacticalCameraController();
        controller.Reset(PlayerSpawn);
        Vector3 initialOffset = controller.Camera.Position - controller.Camera.Target;

        controller.ApplyZoomWheel(100f);
        controller.Update(0.016f, PlayerSpawn);
        Vector3 zoomedInOffset = controller.Camera.Position - controller.Camera.Target;

        controller.ApplyZoomWheel(-100f);
        controller.Update(0.016f, PlayerSpawn);
        Vector3 zoomedOutOffset = controller.Camera.Position - controller.Camera.Target;

        Assert.True(zoomedInOffset.Length() < initialOffset.Length());
        Assert.True(zoomedOutOffset.Length() > initialOffset.Length());
        Assert.Equal(Viewport3DConfig.FollowCameraOffset.Length() * Viewport3DConfig.MinZoom, zoomedInOffset.Length(), 0.05f);
        Assert.Equal(Viewport3DConfig.FollowCameraOffset.Length() * Viewport3DConfig.MaxZoom, zoomedOutOffset.Length(), 0.05f);
    }

    [Fact]
    public void SetTacticalPeek_WhenAimWithinDeadzone_ProducesZeroPeekOffset()
    {
        var controller = new TacticalCameraController();

        controller.SetTacticalPeek(true, PlayerSpawn, PlayerSpawn + new Vector2(3f, 2f));

        Assert.Equal(Vector3.Zero, controller.CurrentPeekOffset);
    }

    [Fact]
    public void SetTacticalPeek_WhenAimExceedsMaxRange_ClampsOffsetToMaxPeekDistance()
    {
        var controller = new TacticalCameraController();

        controller.SetTacticalPeek(true, PlayerSpawn, PlayerSpawn + new Vector2(1000f, 0f));

        Assert.Equal(Viewport3DConfig.MaxPeekDistance, controller.CurrentPeekOffset.X, 0.01f);
        Assert.Equal(0f, controller.CurrentPeekOffset.Y);
        Assert.Equal(0f, controller.CurrentPeekOffset.Z, 0.01f);
    }

    [Fact]
    public void SetTacticalPeek_WhenPeekingInFollowMode_ShiftsFocusTowardAimDirection()
    {
        var controller = new TacticalCameraController();
        controller.Reset(PlayerSpawn);
        Vector3 baseFocus = controller.FocusPoint;

        controller.SetTacticalPeek(true, PlayerSpawn, PlayerSpawn + new Vector2(0f, 200f));
        controller.Update(0.5f, PlayerSpawn);

        Assert.True(controller.FocusPoint.Z > baseFocus.Z);
    }

    [Fact]
    public void SetTacticalPeek_WhenInOverviewMode_SuppressesPeekOffset()
    {
        var controller = new TacticalCameraController();
        controller.ToggleOverviewMode();

        controller.SetTacticalPeek(true, PlayerSpawn, PlayerSpawn + new Vector2(400f, 0f));

        Assert.Equal(Vector3.Zero, controller.CurrentPeekOffset);
    }

    [Fact]
    public void SetTacticalPeek_WhenReleased_ClearsPeekAndReturnsFocusToBaseTarget()
    {
        var controller = new TacticalCameraController();
        controller.Reset(PlayerSpawn);
        Vector3 baseFocus = controller.FocusPoint;

        controller.SetTacticalPeek(true, PlayerSpawn, PlayerSpawn + new Vector2(300f, 0f));
        controller.Update(0.2f, PlayerSpawn);
        Assert.True(controller.FocusPoint.X > baseFocus.X);

        controller.SetTacticalPeek(false, PlayerSpawn, PlayerSpawn + new Vector2(300f, 0f));
        for (int i = 0; i < 40; i++)
        {
            controller.Update(0.05f, PlayerSpawn);
        }

        Assert.Equal(Vector3.Zero, controller.CurrentPeekOffset);
        Assert.Equal(baseFocus.X, controller.FocusPoint.X, 0.1f);
        Assert.Equal(baseFocus.Z, controller.FocusPoint.Z, 0.1f);
    }

    [Fact]
    public void TriggerAlarmShake_AppliesCameraOffsetAndDecaysToZeroOverTime()
    {
        var controller = new TacticalCameraController();
        controller.Reset(PlayerSpawn);

        controller.TriggerAlarmShake(1f);
        controller.Update(0.05f, PlayerSpawn);

        Assert.NotEqual(Vector3.Zero, controller.CurrentShakeOffset);
        Assert.NotEqual(controller.FocusPoint, controller.Camera.Target);

        for (int i = 0; i < 30; i++)
        {
            controller.Update(0.05f, PlayerSpawn);
        }

        Assert.Equal(0f, controller.ShakeTrauma);
        Assert.Equal(Vector3.Zero, controller.CurrentShakeOffset);
        Assert.Equal(controller.FocusPoint, controller.Camera.Target);
    }

    [Fact]
    public void NotifyAlarmState_TriggersOnlyOnRisingEdgeAndResetsOnClear()
    {
        var controller = new TacticalCameraController();
        controller.Reset(PlayerSpawn);

        controller.NotifyAlarmState(true);
        Assert.Equal(1f, controller.ShakeTrauma);

        for (int i = 0; i < 30; i++)
        {
            controller.Update(0.05f, PlayerSpawn);
        }
        Assert.Equal(0f, controller.ShakeTrauma);

        controller.NotifyAlarmState(true);
        Assert.Equal(0f, controller.ShakeTrauma);

        controller.NotifyAlarmState(false);
        controller.NotifyAlarmState(true);
        Assert.Equal(1f, controller.ShakeTrauma);
    }

    [Fact]
    public void GameContext_WhenAlarmTriggeredEventPublished_TriggersCameraShakeAndSurvivesRestart()
    {
        using var context = new GameContext();
        context.InitializeAll();
        context.Camera.Reset(context.PlayerPosition);

        context.EventBus.Publish(new AlarmTriggeredEvent(context.PlayerPosition, "TestLaser"));
        Assert.Equal(1f, context.Camera.ShakeTrauma);

        context.RestartMission();
        Assert.Equal(0f, context.Camera.ShakeTrauma);

        context.EventBus.Publish(new AlarmTriggeredEvent(context.PlayerPosition, "TestGuard"));
        Assert.Equal(1f, context.Camera.ShakeTrauma);
    }
}
