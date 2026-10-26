using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.WorldLighting;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class WorldLightingTests
{
    private const float FloatTolerance = 0.001f;

    [Fact]
    public void LoadFromJson_ValidJson_PopulatesMapDataSpawnsAndContext()
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
          "interactables": [
            { "type": "LootCrate", "id": "loot_1", "x": 960, "y": 192, "cashValue": 250000 }
          ],
          "guards": [
            {
              "name": "Guard_Lobby",
              "waypoints": [
                { "x": 320, "y": 320 },
                { "x": 640, "y": 320 }
              ]
            }
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

        Assert.Single(mapData.Interactables);
        Assert.Equal("LootCrate", mapData.Interactables[0].Type);
        Assert.Equal(250_000, mapData.Interactables[0].CashValue);

        Assert.Single(mapData.Guards);
        Assert.Equal("Guard_Lobby", mapData.Guards[0].Name);
        Assert.Equal(2, mapData.Guards[0].Waypoints.Count);
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
}
