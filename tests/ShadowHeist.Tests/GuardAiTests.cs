using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI;
using ShadowHeist.GuardAI.Pathfinding;
using ShadowHeist.GuardAI.States;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Unit tests for Guard entity, PatrolState, SuspiciousState (Ticket #13), and AStarPathfinder (Ticket #14).
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

    [Fact]
    public void Guard_TransitionsToSuspiciousState_WhenNoiseEmittedWithinRadius()
    {
        var waypoints = new List<Vector2> { new(100f, 100f), new(200f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints);
        var noise = new NoiseEmittedEvent(new Vector2(220f, 100f), Radius: 150f, SourceName: "Sprint");

        bool heard = guard.OnNoiseEmitted(noise);

        Assert.True(heard);
        var suspicious = Assert.IsType<SuspiciousState>(guard.CurrentState);
        Assert.Equal("Suspicious", suspicious.StateName);
        Assert.Equal(new Vector2(220f, 100f), suspicious.InvestigationTarget);
    }

    [Fact]
    public void Guard_IgnoresNoiseEmittedEvent_WhenOutsideHearingRadius()
    {
        var waypoints = new List<Vector2> { new(100f, 100f), new(200f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints);
        var distantNoise = new NoiseEmittedEvent(new Vector2(500f, 500f), Radius: 80f, SourceName: "Walk");

        bool heard = guard.OnNoiseEmitted(distantNoise);

        Assert.False(heard);
        Assert.IsType<PatrolState>(guard.CurrentState);
    }

    [Fact]
    public void SuspiciousState_InvestigatesTarget_AndReturnsToPatrolWhenTimerExpires()
    {
        using var context = new GameContext();
        var waypoints = new List<Vector2> { new(100f, 100f), new(200f, 100f) };
        var guard = new Guard("guard_1", new Vector2(100f, 100f), waypoints, moveSpeed: 100f);

        guard.OnNoiseEmitted(new NoiseEmittedEvent(new Vector2(150f, 100f), Radius: 200f, SourceName: "Coin"));
        var suspicious = Assert.IsType<SuspiciousState>(guard.CurrentState);

        // Move to investigation target (50 units away at 100 * 1.15 = 115 units/s)
        guard.Update(0.5f, context);
        Assert.True(suspicious.IsInvestigating);
        Assert.Equal(new Vector2(150f, 100f), guard.Position);

        // Scan duration (default 2.5s) expires -> guard returns to PatrolState
        guard.Update(2.6f, context);
        Assert.IsType<PatrolState>(guard.CurrentState);
    }

    [Fact]
    public void GuardAiModule_SubscribesToEventBus_AndTransitionsNearbyGuardToSuspicious()
    {
        using var context = new GameContext();
        using var module = new GuardAiModule();
        module.Init(context);

        // Alpha guard spawns at (320, 200); emit noise nearby at (360, 200)
        context.EventBus.Publish(new NoiseEmittedEvent(new Vector2(360f, 200f), Radius: 100f, SourceName: "PlayerFootstep:Sprint"));

        Assert.IsType<SuspiciousState>(module.Guards[0].CurrentState);
        Assert.IsType<PatrolState>(module.Guards[1].CurrentState);
    }

    [Fact]
    public void AStarPathfinder_FindsDetourPathAroundBlockingWall()
    {
        // Vertical wall blocking direct horizontal line between (80, 160) and (240, 160)
        var walls = new List<RectangleBounds>
        {
            new(144f, 64f, 32f, 224f)
        };

        var start = new Vector2(80f, 160f);
        var goal = new Vector2(240f, 160f);

        IReadOnlyList<Vector2> path = AStarPathfinder.FindPath(start, goal, walls);

        Assert.NotEmpty(path);
        Assert.True(path.Count > 1);
        Assert.Equal(goal, path[^1]);

        // Verify at least one waypoint detours above (< 64) or below (> 288) the vertical wall
        Assert.Contains(path, wp => wp.Y < 64f || wp.Y > 288f);
    }

    [Fact]
    public void AStarPathfinder_ReturnsEmptyPath_WhenGoalIsSealedInsideWalls()
    {
        // Sealed box around (300, 300)
        var walls = new List<RectangleBounds>
        {
            new(224f, 224f, 160f, 32f),
            new(224f, 352f, 160f, 32f),
            new(224f, 224f, 32f, 160f),
            new(352f, 224f, 32f, 160f)
        };

        var graph = new GridGraph(walls, cellSize: 32f, mapWidth: 640f, mapHeight: 640f, clearance: 4f);
        IReadOnlyList<Vector2> path = AStarPathfinder.FindPath(graph, new Vector2(64f, 64f), new Vector2(304f, 304f));

        Assert.Empty(path);
    }
}
