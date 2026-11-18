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
        Assert.NotNull(mapData.Lasers);
        Assert.Single(mapData.Lasers!);
        Assert.Equal("laser_hall", mapData.Lasers![0].Id);
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
    public void LoadFromJson_WithObstacles_PopulatesMapObstaclesAndCollisionWalls()
    {
        using var context = new GameContext();
        const string json = """
        {
          "levelName": "Bank Obstacles",
          "tileSize": 64,
          "width": 20,
          "height": 12,
          "obstacles": [
            { "kind": "sofa", "x": 74, "y": 388, "width": 44, "height": 104 }
          ]
        }
        """;

        LevelMapData mapData = MapLoader.LoadFromJson(json, context);

        Assert.Single(mapData.Obstacles!);
        Assert.Contains(new RectangleBounds(74f, 388f, 44f, 104f), context.Walls);
    }

    [Fact]
    public void HasLineOfSight_ReturnsFalse_WhenWallIsBetweenPoints()
    {
        var walls = new[] { new RectangleBounds(150f, 50f, 32f, 200f) };

        Assert.False(CollisionHelper.HasLineOfSight(new Vector2(100f, 150f), new Vector2(220f, 150f), walls));
        Assert.True(CollisionHelper.HasLineOfSight(new Vector2(100f, 150f), new Vector2(130f, 150f), walls));
    }

    [Fact]
    public void Level1CityBankJson_HasNoWallOverlapsWithObstaclesOrGuardWaypointsAndHasValidLasers()
    {
        using var context = new GameContext();
        string mapPath = Path.Combine(AppContext.BaseDirectory, "assets", "maps", "level1_city_bank.json");
        LevelMapData mapData = MapLoader.LoadFromFile(mapPath, context);

        Assert.NotNull(mapData.Obstacles);
        Assert.NotNull(mapData.Guards);
        Assert.NotNull(mapData.Interactables);
        Assert.NotNull(mapData.Lasers);
        Assert.NotEmpty(mapData.Lasers!);

        foreach (MapPropObstacle obstacle in mapData.Obstacles!)
        {
            RectangleBounds ob = obstacle.ToBounds();
            foreach (RectangleBounds wall in mapData.Walls)
            {
                bool overlaps = ob.X < wall.X + wall.Width && ob.X + ob.Width > wall.X &&
                                ob.Y < wall.Y + wall.Height && ob.Y + ob.Height > wall.Y;
                Assert.False(overlaps, $"Obstacle {obstacle.Kind} at ({ob.X},{ob.Y}) overlaps wall ({wall.X},{wall.Y}).");
            }
        }

        foreach (MapGuardDto guard in mapData.Guards!)
        {
            foreach (Vector2 wp in guard.Waypoints)
            {
                Vector2 resolved = CollisionHelper.ResolveWallCollision(wp, CollisionHelper.DefaultActorRadius, context.Walls);
                Assert.Equal(wp, resolved);
            }
        }

        foreach (LaserSpawnData spawn in mapData.Lasers!)
        {
            var laser = new LaserTripwire(spawn.Id, spawn.Origin, spawn.Direction);
            laser.Update(context);
            Assert.NotEqual(laser.Origin, laser.BeamEnd);
        }

        Assert.NotNull(mapData.GlassHazards);
        Assert.NotEmpty(mapData.GlassHazards!);
        foreach (GlassHazardSpawnData glass in mapData.GlassHazards!)
        {
            RectangleBounds gb = glass.Bounds;
            foreach (RectangleBounds wall in mapData.Walls)
            {
                bool overlaps = gb.X < wall.X + wall.Width && gb.X + gb.Width > wall.X &&
                                gb.Y < wall.Y + wall.Height && gb.Y + gb.Height > wall.Y;
                Assert.False(overlaps, $"Glass hazard {glass.Id} at ({gb.X},{gb.Y}) overlaps wall ({wall.X},{wall.Y}).");
            }
        }
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

    [Fact]
    public void LaserTripwire_WhenPlayerPassesThroughBeamBetweenFrames_TriggersAlarm()
    {
        using var context = new GameContext();
        context.Walls.Add(new RectangleBounds(0f, 500f, 600f, 32f));
        var laser = new LaserTripwire("laser_vault", new Vector2(200f, 100f), Vector2.UnitY);

        context.PlayerPosition = new Vector2(170f, 300f);
        laser.Update(context, actorRadius: 12f);
        Assert.False(laser.HasTriggeredAlarm);

        context.PlayerPosition = new Vector2(230f, 300f);
        laser.Update(context, actorRadius: 12f);

        Assert.True(laser.HasTriggeredAlarm);
        Assert.True(context.IsAlarmTriggered);
    }

    [Fact]
    public void BrokenGlassTile_WhenPlayerStepsOntoAndWalksAcrossGlass_EmitsFootstepNoiseSpikes()
    {
        using var context = new GameContext();
        var glassTile = new BrokenGlassTile(
            "glass_corridor",
            new RectangleBounds(100f, 100f, 80f, 60f),
            noiseRadius: 220f,
            stepTriggerDistance: 20f);

        var emittedEvents = new List<NoiseEmittedEvent>();
        context.EventBus.Subscribe<NoiseEmittedEvent>(evt => emittedEvents.Add(evt));

        context.PlayerPosition = new Vector2(50f, 50f);
        Assert.False(glassTile.Update(context));
        Assert.Empty(emittedEvents);

        context.PlayerPosition = new Vector2(110f, 120f);
        Assert.True(glassTile.Update(context));
        Assert.Single(emittedEvents);
        Assert.Equal("BrokenGlass:glass_corridor", emittedEvents[0].SourceName);
        Assert.Equal(NoiseCategory.Player, emittedEvents[0].Category);
        Assert.Equal(220f, emittedEvents[0].Radius, FloatTolerance);

        context.PlayerPosition = new Vector2(118f, 120f);
        Assert.False(glassTile.Update(context));
        Assert.Single(emittedEvents);

        context.PlayerPosition = new Vector2(140f, 120f);
        Assert.True(glassTile.Update(context));
        Assert.Equal(2, emittedEvents.Count);
    }

    [Fact]
    public void ShadowLightingController_WhenPowerCutAndInsideVault_IncreasesAmbientAndZoneDarkness()
    {
        using var context = new GameContext();
        var controller = new ShadowLightingController();
        var lampPosition = new Vector2(84f, 344f);
        var vaultPosition = new Vector2(500f, 280f);

        float litLobbyDarkness = controller.GetZoneDarkness(lampPosition);
        float poweredVaultDarkness = controller.GetZoneDarkness(vaultPosition);

        Assert.True(poweredVaultDarkness > litLobbyDarkness);

        controller.SetPowerState(isPowered: false);
        controller.Update(deltaTime: 1.0f, context);

        Assert.False(controller.IsMainPowerOnline);
        Assert.True(controller.AmbientDarknessIntensity > ShadowLightingController.DefaultPoweredDarkness);
        Assert.True(controller.GetZoneDarkness(lampPosition) > litLobbyDarkness);
    }

    [Fact]
    public void LoadFromJson_WithGlassHazards_PopulatesGlassHazardSpawns()
    {
        using var context = new GameContext();
        const string json = """
        {
          "levelName": "Bank Glass Hazards",
          "tileSize": 64,
          "width": 20,
          "height": 12,
          "glassHazards": [
            { "id": "glass_hall", "x": 420, "y": 580, "width": 60, "height": 44 }
          ]
        }
        """;

        LevelMapData mapData = MapLoader.LoadFromJson(json, context);

        Assert.NotNull(mapData.GlassHazards);
        Assert.Single(mapData.GlassHazards!);
        Assert.Equal("glass_hall", mapData.GlassHazards![0].Id);
        Assert.Equal(new RectangleBounds(420f, 580f, 60f, 44f), mapData.GlassHazards[0].Bounds);
    }
}
