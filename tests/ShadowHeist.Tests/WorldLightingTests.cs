using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.WorldLighting;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class WorldLightingTests
{
    private const float FloatTolerance = 0.001f;

    [Fact]
    public void LoadFromJson_ValidJson_PopulatesMapDataAndContext()
    {
        using var context = new GameContext();
        const string json = """
        {
          "levelName": "Level 1: City Central Bank",
          "tileSize": 64,
          "width": 20,
          "height": 12,
          "playerSpawn": { "x": 128, "y": 640 },
          "getawayZone": { "x": 64, "y": 640, "width": 128, "height": 96 },
          "walls": [
            { "x": 0, "y": 0, "width": 1280, "height": 32 },
            { "x": 320, "y": 160, "width": 640, "height": 32 }
          ],
          "lasers": [
            { "id": "laser_hall", "x": 336, "y": 386, "direction": { "x": 0, "y": 1 } }
          ]
        }
        """;

        LevelMapData mapData = MapLoader.LoadFromJson(json, context);

        Assert.Equal("Level 1: City Central Bank", mapData.LevelName);
        Assert.Equal(64, mapData.TileSize);
        Assert.Equal(20, mapData.GridWidth);
        Assert.Equal(12, mapData.GridHeight);
        Assert.Equal(new Vector2(128f, 640f), context.PlayerPosition);
        Assert.Equal(new RectangleBounds(64f, 640f, 128f, 96f), mapData.GetawayZone);
        Assert.Equal(2, context.Walls.Count);
        Assert.Single(mapData.Lasers);
        Assert.Equal("laser_hall", mapData.Lasers[0].Id);
        Assert.Equal(new Vector2(336f, 386f), mapData.Lasers[0].Origin);
    }

    [Fact]
    public void LoadFromJson_WhenWallsArrayOmitted_BuildsDefaultPerimeterAndVaultWalls()
    {
        using var context = new GameContext();
        const string json = """
        {
          "levelName": "Level 1: City Central Bank",
          "tileSize": 64,
          "width": 20,
          "height": 12,
          "playerSpawn": { "x": 128, "y": 640 }
        }
        """;

        LevelMapData mapData = MapLoader.LoadFromJson(json, context);

        Assert.Equal(new Vector2(128f, 640f), mapData.PlayerSpawn);
        Assert.NotEmpty(context.Walls);
        Assert.True(context.Walls.Count >= 4);
    }

    [Fact]
    public void ResolveWallCollision_WhenActorIsOutsideWall_ReturnsUnchangedPosition()
    {
        var wall = new RectangleBounds(200f, 200f, 100f, 100f);
        var actorPosition = new Vector2(100f, 100f);
        const float actorRadius = 20f;

        Vector2 resolvedPosition = CollisionHelper.ResolveWallCollision(actorPosition, actorRadius, new[] { wall });

        Assert.Equal(actorPosition, resolvedPosition);
    }

    [Fact]
    public void ResolveWallCollision_WhenActorOverlapsLeftWallEdge_PushesActorOutwardLeft()
    {
        var wall = new RectangleBounds(100f, 100f, 200f, 50f);
        var actorPosition = new Vector2(90f, 125f);
        const float actorRadius = 20f;

        Vector2 resolvedPosition = CollisionHelper.ResolveWallCollision(actorPosition, actorRadius, new[] { wall });

        Assert.Equal(80f, resolvedPosition.X, FloatTolerance);
        Assert.Equal(125f, resolvedPosition.Y, FloatTolerance);
    }

    [Fact]
    public void ResolveWallCollision_WhenActorMovesDiagonallyIntoHorizontalWall_SlidesAlongXAxis()
    {
        var topWall = new RectangleBounds(0f, 0f, 1280f, 32f);
        var actorPosition = new Vector2(350f, 40f);
        const float actorRadius = 18f;

        Vector2 resolvedPosition = CollisionHelper.ResolveWallCollision(actorPosition, actorRadius, new[] { topWall });

        Assert.Equal(350f, resolvedPosition.X, FloatTolerance);
        Assert.Equal(50f, resolvedPosition.Y, FloatTolerance);
    }

    [Fact]
    public void ResolveWallCollision_WhenActorCenterIsInsideWall_EjectsToNearestEdge()
    {
        var wall = new RectangleBounds(100f, 100f, 100f, 100f);
        var actorPosition = new Vector2(150f, 105f);
        const float actorRadius = 15f;

        Vector2 resolvedPosition = CollisionHelper.ResolveWallCollision(actorPosition, actorRadius, new[] { wall });

        Assert.Equal(150f, resolvedPosition.X, FloatTolerance);
        Assert.Equal(85f, resolvedPosition.Y, FloatTolerance);
    }

    [Fact]
    public void CastRay_WhenWallBlocksRay_ClipsRayAtNearestWallEdge()
    {
        var origin = new Vector2(100f, 100f);
        var direction = Vector2.UnitX;
        var wall = new RectangleBounds(220f, 50f, 40f, 100f);

        Vector2 hitPoint = VisionRaycaster.CastRay(origin, direction, maxDistance: 500f, new[] { wall });

        Assert.Equal(220f, hitPoint.X, FloatTolerance);
        Assert.Equal(100f, hitPoint.Y, FloatTolerance);
    }

    [Fact]
    public void CastRay_WhenNoWallBlocksRay_ReachesFullMaxDistance()
    {
        var origin = new Vector2(100f, 100f);
        var direction = Vector2.UnitY;
        var distantWall = new RectangleBounds(500f, 500f, 64f, 64f);

        Vector2 hitPoint = VisionRaycaster.CastRay(origin, direction, maxDistance: 200f, new[] { distantWall });

        Assert.Equal(100f, hitPoint.X, FloatTolerance);
        Assert.Equal(300f, hitPoint.Y, FloatTolerance);
    }

    [Fact]
    public void CastRay_WhenOriginIsInsideWall_ReturnsOriginImmediately()
    {
        var wall = new RectangleBounds(100f, 100f, 80f, 80f);
        var originInsideWall = new Vector2(140f, 140f);

        Vector2 hitPoint = VisionRaycaster.CastRay(originInsideWall, Vector2.UnitX, maxDistance: 300f, new[] { wall });

        Assert.Equal(originInsideWall, hitPoint);
    }

    [Fact]
    public void ComputeVisionCone_ReturnsOriginPlusClippedFanVertices()
    {
        var origin = new Vector2(100f, 100f);
        var wallInFront = new RectangleBounds(200f, 0f, 32f, 300f);
        const int rayCount = 8;

        IReadOnlyList<Vector2> coneVertices = VisionRaycaster.ComputeVisionCone(
            origin,
            facingDegrees: 0f,
            new[] { wallInFront },
            fovDegrees: 60f,
            maxDistance: 300f,
            rayCount: rayCount);

        Assert.Equal(rayCount + 2, coneVertices.Count);
        Assert.Equal(origin, coneVertices[0]);
        Assert.All(coneVertices, vertex => Assert.True(vertex.X <= 200f + FloatTolerance));
    }

    [Fact]
    public void LaserTripwire_WhenPlayerCrossesActiveBeam_TriggersAlarmAndPublishesEvent()
    {
        using var context = new GameContext();
        context.Walls.Add(new RectangleBounds(0f, 500f, 600f, 32f));
        context.PlayerPosition = new Vector2(205f, 300f);

        AlarmTriggeredEvent? publishedEvent = null;
        context.EventBus.Subscribe<AlarmTriggeredEvent>(evt => publishedEvent = evt);

        var laser = new LaserTripwire("laser_vault", new Vector2(200f, 100f), Vector2.UnitY);

        laser.Update(context, actorRadius: 18f);

        Assert.Equal(new Vector2(200f, 500f), laser.BeamEnd);
        Assert.True(laser.HasTriggeredAlarm);
        Assert.True(context.IsAlarmTriggered);
        Assert.NotNull(publishedEvent);
        Assert.Equal("LaserTripwire:laser_vault", publishedEvent.Value.Reason);
    }

    [Fact]
    public void LaserTripwire_WhenUpdatedMultipleTimesOnBeam_PublishesEventOnlyOnce()
    {
        using var context = new GameContext();
        context.Walls.Add(new RectangleBounds(0f, 500f, 600f, 32f));
        context.PlayerPosition = new Vector2(200f, 300f);

        int publishedCount = 0;
        context.EventBus.Subscribe<AlarmTriggeredEvent>(_ => publishedCount++);

        var laser = new LaserTripwire("laser_vault", new Vector2(200f, 100f), Vector2.UnitY);

        laser.Update(context, actorRadius: 18f);
        laser.Update(context, actorRadius: 18f);

        Assert.Equal(1, publishedCount);
        Assert.True(laser.HasTriggeredAlarm);
    }

    [Fact]
    public void LaserTripwire_WhenPlayerIsOutsideBeamOrLaserInactive_DoesNotTriggerAlarm()
    {
        using var context = new GameContext();
        context.Walls.Add(new RectangleBounds(0f, 500f, 600f, 32f));
        context.PlayerPosition = new Vector2(400f, 300f);

        var laser = new LaserTripwire("laser_vault", new Vector2(200f, 100f), Vector2.UnitY);
        laser.Update(context, actorRadius: 18f);

        Assert.False(laser.HasTriggeredAlarm);
        Assert.False(context.IsAlarmTriggered);

        laser.IsActive = false;
        context.PlayerPosition = new Vector2(200f, 250f);
        laser.Update(context, actorRadius: 18f);

        Assert.False(laser.HasTriggeredAlarm);
        Assert.False(context.IsAlarmTriggered);
    }
}
