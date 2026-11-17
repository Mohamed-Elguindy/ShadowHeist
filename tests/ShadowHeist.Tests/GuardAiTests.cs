using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.GuardAI;
using ShadowHeist.GuardAI.States;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Unit tests for Guard entity, PatrolState waypoint movement, angle rotation, vision alerts, and laser alarm investigation.
/// </summary>
public sealed class GuardAiTests
{
    [Fact]
    public void Guard_InitializesInPatrolState_AndAtInitialPosition()
    {
        var waypoints = new List<Vector2> { new(100f, 100f), new(200f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints, moveSpeed: 80f);

        Assert.Equal("guard_1", guard.Id);
        Assert.Equal(new Vector2(100f, 100f), guard.Position);
        Assert.Equal("Patrol", guard.CurrentState.StateName);
        Assert.IsType<PatrolState>(guard.CurrentState);
    }

    [Fact]
    public void PatrolState_MovesGuardTowardsTargetWaypoint()
    {
        using var context = new GameContext();
        var waypoints = new List<Vector2> { new(200f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints, moveSpeed: 100f);

        guard.Update(0.5f, context);

        Assert.True(guard.Position.X > 100f);
        Assert.Equal(150f, guard.Position.X, precision: 1);
        Assert.Equal(100f, guard.Position.Y, precision: 1);
    }

    [Fact]
    public void PatrolState_ReachesWaypoint_AndEntersWaitingState()
    {
        using var context = new GameContext();
        var waypoints = new List<Vector2> { new(105f, 100f), new(200f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints, moveSpeed: 100f);

        guard.Update(0.1f, context);

        var patrolState = Assert.IsType<PatrolState>(guard.CurrentState);
        Assert.Equal(new Vector2(105f, 100f), guard.Position);
        Assert.True(patrolState.IsWaiting);
        Assert.True(patrolState.WaitTimer > 0f);
    }

    [Fact]
    public void PatrolState_AdvancesToNextWaypoint_AfterWaitDurationExpires()
    {
        using var context = new GameContext();
        var waypoints = new List<Vector2> { new(100f, 100f), new(300f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints, moveSpeed: 100f);

        guard.Update(0.01f, context);
        var patrolState = Assert.IsType<PatrolState>(guard.CurrentState);
        Assert.True(patrolState.IsWaiting);

        guard.Update(1.6f, context);

        Assert.False(patrolState.IsWaiting);
        Assert.Equal(1, patrolState.CurrentWaypointIndex);
        Assert.Equal(new Vector2(300f, 100f), patrolState.TargetWaypoint);
    }

    [Fact]
    public void PatrolState_CyclesBackToStart_WhenReachingEnd()
    {
        using var context = new GameContext();
        var waypoints = new List<Vector2> { new(100f, 100f), new(200f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints, moveSpeed: 1000f);

        var patrolState = Assert.IsType<PatrolState>(guard.CurrentState);

        guard.Update(0.01f, context);
        guard.Update(1.6f, context);
        Assert.Equal(1, patrolState.CurrentWaypointIndex);

        guard.Update(0.2f, context);
        Assert.True(patrolState.IsWaiting);

        guard.Update(1.6f, context);
        Assert.False(patrolState.IsWaiting);
        Assert.Equal(0, patrolState.CurrentWaypointIndex);
    }

    [Fact]
    public void Guard_RotateToward_SmoothlyInterpolatesDegrees()
    {
        var waypoints = new List<Vector2> { new(100f, 100f) };
        var guard = new Guard("guard_1", new Vector2(0f, 0f), waypoints);
        guard.RotationDegrees = 0f;

        guard.RotateToward(new Vector2(0f, 1f), 0.25f, turnRate: 180f);

        Assert.Equal(45f, guard.RotationDegrees, precision: 1);
    }

    [Fact]
    public void MoveTowardsAngle_HandlesShortestPathAcrossZeroBoundary()
    {
        float result = Guard.MoveTowardsAngle(350f, 10f, 15f);

        float normalized = (result + 360f) % 360f;
        Assert.Equal(5f, normalized, precision: 1);
    }

    [Fact]
    public void Guard_TransitionsToAlertAndTriggersAlarm_WhenPlayerVisibleInCone()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(200f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), new[] { new Vector2(400f, 100f) })
        {
            RotationDegrees = 0f
        };

        guard.Update(0.05f, context);

        Assert.IsType<AlertChaseState>(guard.CurrentState);
        Assert.True(context.IsAlarmTriggered);
    }

    [Fact]
    public void Guard_DoesNotAlert_WhenWallBlocksLineOfSight()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(220f, 100f) };
        context.Walls.Add(new RectangleBounds(150f, 60f, 20f, 80f));
        var guard = new Guard("guard_1", new Vector2(100f, 100f), new[] { new Vector2(400f, 100f) })
        {
            RotationDegrees = 0f
        };

        guard.Update(0.05f, context);

        Assert.IsType<PatrolState>(guard.CurrentState);
        Assert.False(context.IsAlarmTriggered);
    }

    [Fact]
    public void Guard_TransitionsToSuspicious_WhenNoiseNotified()
    {
        using var context = new GameContext();
        var guard = new Guard("guard_1", new Vector2(100f, 100f), new[] { new Vector2(400f, 100f) });
        guard.NotifyNoise(new Vector2(180f, 150f));

        guard.Update(0.05f, context);

        Assert.IsType<SuspiciousState>(guard.CurrentState);
    }

    [Fact]
    public void AlertChaseState_TriggersCaughtOutcome_WhenGuardReachesPlayer()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(110f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), new[] { new Vector2(400f, 100f) });

        guard.Update(0.05f, context);
        guard.Update(0.05f, context);

        Assert.Equal(HeistOutcome.Caught, context.Outcome);
    }

    [Fact]
    public void GuardAiModule_GuardsInvestigate_WhenLaserTripwireAlarmTriggers()
    {
        using var context = new GameContext();
        using var guardModule = new GuardAiModule();
        guardModule.Init(context);

        context.EventBus.Publish(new AlarmTriggeredEvent(new Vector2(336f, 500f), "LaserTripwire:laser_hall"));
        guardModule.Update(0.05f, context);

        Assert.All(guardModule.Guards, g => Assert.IsType<SuspiciousState>(g.CurrentState));
    }
}
