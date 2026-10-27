using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI;
using ShadowHeist.GuardAI.States;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Unit tests for Guard entity, PatrolState waypoint movement, and angle rotation interpolation.
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
        var waypoints = new List<Vector2> { new(200f, 100f) }; // Target is 100 units to the right
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints, moveSpeed: 100f);

        // Advance 0.5s at 100 units/s -> should move 50 units towards (200, 100)
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

        // Target is 5 units away, which is within reach threshold (< 6f)
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

        // Guard starts on waypoint 0 -> immediate reach & wait
        guard.Update(0.01f, context);
        var patrolState = Assert.IsType<PatrolState>(guard.CurrentState);
        Assert.True(patrolState.IsWaiting);

        // Wait timer expires (default wait duration is 1.5s)
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

        // Guard is at WP 0. Complete initial wait and switch target to WP 1
        guard.Update(0.01f, context); // Reach WP 0 -> enters wait
        guard.Update(1.6f, context);  // Wait timer expires -> target becomes WP 1
        Assert.Equal(1, patrolState.CurrentWaypointIndex);

        // Move guard to WP 1 (distance 100 units, speed 1000 units/s)
        guard.Update(0.2f, context);  // Reaches WP 1 -> enters wait
        Assert.True(patrolState.IsWaiting);

        // Wait timer expires at WP 1 -> cycles back to WP 0
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

        // Rotate toward pointing down (0, 1) -> target is 90 degrees
        // Turn rate 180 deg/s * 0.25s = 45 degrees
        guard.RotateToward(new Vector2(0f, 1f), 0.25f, turnRate: 180f);

        Assert.Equal(45f, guard.RotationDegrees, precision: 1);
    }

    [Fact]
    public void MoveTowardsAngle_HandlesShortestPathAcrossZeroBoundary()
    {
        // Rotating from 350 degrees to 10 degrees across the 0/360 wrap
        float result = Guard.MoveTowardsAngle(350f, 10f, 15f);

        // Shortest distance is +20 degrees. Stepping by 15 brings it to 365 => 5 degrees
        float normalized = (result + 360f) % 360f;
        Assert.Equal(5f, normalized, precision: 1);
    }
}
