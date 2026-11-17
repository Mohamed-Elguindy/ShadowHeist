using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.GuardAI;
using ShadowHeist.GuardAI.Pathfinding;
using ShadowHeist.GuardAI.States;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Unit tests for SuspiciousState noise investigation (Ticket #13) and AStarPathfinder (Ticket #14).
/// </summary>
public sealed class GuardPathfindingTests
{
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

        guard.Update(0.5f, context);
        Assert.True(suspicious.IsInvestigating);
        Assert.Equal(new Vector2(150f, 100f), guard.Position);

        guard.Update(2.6f, context);
        Assert.IsType<PatrolState>(guard.CurrentState);
    }

    [Fact]
    public void GuardAiModule_SubscribesToEventBus_AndTransitionsNearbyGuardToSuspicious()
    {
        using var context = new GameContext();
        using var module = new GuardAiModule();
        module.Init(context);

        context.EventBus.Publish(new NoiseEmittedEvent(new Vector2(360f, 200f), Radius: 100f, SourceName: "PlayerFootstep:Sprint"));

        Assert.IsType<SuspiciousState>(module.Guards[0].CurrentState);
        Assert.IsType<PatrolState>(module.Guards[1].CurrentState);
    }

    [Fact]
    public void AStarPathfinder_FindsDetourPathAroundBlockingWall()
    {
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
        Assert.Contains(path, waypoint => waypoint.Y < 64f || waypoint.Y > 288f);
    }

    [Fact]
    public void AStarPathfinder_ReturnsEmptyPath_WhenGoalIsSealedInsideWalls()
    {
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
