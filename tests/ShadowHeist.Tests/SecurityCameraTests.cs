using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.GuardAI;
using ShadowHeist.GuardAI.States;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Unit tests for SecurityCamera sweeping vision cone, FuseBox/EMP disable, and AlertChaseState A* edge cases (Tickets #29 &amp; #30 — Hatem).
/// </summary>
public sealed class SecurityCameraTests
{
    [Fact]
    public void SecurityCamera_Update_SweepsAngleWithinConfiguredAmplitude()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(900f, 900f) };
        var camera = new SecurityCamera("cam_1", new Vector2(100f, 100f), centerAngleDegrees: 90f, sweepAmplitudeDegrees: 30f);

        camera.Update(0.5f, context);

        Assert.NotEqual(90f, camera.CurrentAngleDegrees);
        Assert.InRange(camera.CurrentAngleDegrees, 60f, 120f);
    }

    [Fact]
    public void SecurityCamera_DetectsPlayerInCone_AndPublishesAlarmTriggeredEvent()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(180f, 100f) };
        var camera = new SecurityCamera(
            "cam_vault",
            new Vector2(100f, 100f),
            centerAngleDegrees: 0f,
            sweepAmplitudeDegrees: 0f);

        AlarmTriggeredEvent? capturedAlarm = null;
        context.EventBus.Subscribe<AlarmTriggeredEvent>(evt => capturedAlarm = evt);

        camera.Update(0.3f, context);
        Assert.True(camera.DetectionProgress > 0f && camera.DetectionProgress < 1f);
        Assert.False(context.IsAlarmTriggered);

        bool triggered = camera.Update(1.0f, context);

        Assert.True(triggered);
        Assert.Equal(1f, camera.DetectionProgress);
        Assert.True(context.IsAlarmTriggered);
        Assert.NotNull(capturedAlarm);
        Assert.Equal("SecurityCamera:cam_vault", capturedAlarm.Value.Reason);
    }

    [Fact]
    public void SecurityCamera_DoesNotDetectPlayer_WhenWallBlocksLineOfSight()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(220f, 100f) };
        context.Walls.Add(new RectangleBounds(150f, 60f, 20f, 80f));
        var camera = new SecurityCamera("cam_1", new Vector2(100f, 100f), centerAngleDegrees: 0f, sweepAmplitudeDegrees: 0f);

        camera.Update(1.0f, context);

        Assert.Equal(0f, camera.DetectionProgress);
        Assert.False(context.IsAlarmTriggered);
    }

    [Fact]
    public void SecurityCamera_DisablesAndClearsVisionCone_WhenFuseBoxCutsPower()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(160f, 100f) };
        var camera = new SecurityCamera("cam_lobby", new Vector2(100f, 100f), centerAngleDegrees: 0f, sweepAmplitudeDegrees: 0f);

        camera.Update(0.2f, context);
        Assert.True(camera.DetectionProgress > 0f);
        Assert.NotEmpty(camera.ComputeVisionCone(context.Walls));

        camera.ApplyPowerState(new PowerStateChangedEvent("lobby_lights", IsPowered: false));

        Assert.False(camera.IsActive);
        Assert.Equal(0f, camera.DetectionProgress);
        Assert.Empty(camera.ComputeVisionCone(context.Walls));
    }

    [Fact]
    public void SecurityCamera_DisableTemporarily_StunsCameraForEmpDurationAndRecovers()
    {
        using var context = new GameContext();
        var camera = new SecurityCamera("cam_1", new Vector2(100f, 100f), centerAngleDegrees: 0f);

        camera.DisableTemporarily(3.0f);
        Assert.False(camera.IsActive);

        camera.Update(1.5f, context);
        Assert.False(camera.IsActive);

        camera.Update(2.0f, context);
        Assert.True(camera.IsActive);
    }

    [Fact]
    public void GuardAiModule_WhenSecurityCameraTriggersAlarm_DispatchesGuardsToInvestigate()
    {
        using var context = new GameContext();
        using var guardModule = new GuardAiModule();
        guardModule.Init(context);

        context.EventBus.Publish(new AlarmTriggeredEvent(new Vector2(314f, 260f), "SecurityCamera:cam_lobby_west"));
        guardModule.Update(0.05f, context);

        Assert.All(guardModule.Guards, guard => Assert.IsType<SuspiciousState>(guard.CurrentState));
    }

    [Fact]
    public void AlertChaseState_RepathsAroundWall_AndReturnsToPatrolAfterSearchTimeout()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(260f, 160f) };
        context.Walls.Add(new RectangleBounds(160f, 64f, 32f, 224f));

        var patrolRoute = new List<Vector2> { new(80f, 160f), new(80f, 260f) };
        var guard = new Guard("guard_chase", new Vector2(80f, 160f), patrolRoute);
        var chaseState = new AlertChaseState(guard, patrolRoute, context.PlayerPosition);
        guard.TransitionTo(chaseState);

        guard.Update(0.1f, context);
        Assert.Equal(1f, guard.DetectionProgress);
        Assert.True(chaseState.PathWaypoints.Count > 1);

        // Move player out of sight behind wall and let 5s search timeout expire
        context.PlayerPosition = new Vector2(300f, 160f);
        guard.Update(5.2f, context);

        Assert.IsType<PatrolState>(guard.CurrentState);
        Assert.Equal(0f, guard.DetectionProgress);
    }
}
