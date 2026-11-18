using System.Numerics;
using System.Text.Json;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting;

public static class MapLoader
{
    private const string DefaultLevelName = "Level 1: City Central Bank";
    private const string DefaultHallLaserId = "laser_hall";
    private const string DefaultCorridorGlassId = "glass_corridor_east";
    private const string DefaultLobbyGlassId = "glass_lobby_south";
    private const int DefaultTileSize = 64;
    private const int DefaultGridWidth = 20;
    private const int DefaultGridHeight = 12;
    private const float OuterWallThickness = 32f;

    private static readonly Vector2 DefaultPlayerSpawn = new(128f, 640f);
    private static readonly RectangleBounds DefaultGetawayZone = HeistConstants.DefaultGetawayZone;
    private static readonly RectangleBounds DefaultVaultTopWall = new(320f, 160f, 640f, 32f);
    private static readonly RectangleBounds DefaultVaultBottomWall = new(320f, 384f, 640f, 32f);
    private static readonly RectangleBounds DefaultLobbyDividerWall = new(320f, 160f, 32f, 256f);
    private static readonly RectangleBounds DefaultVaultEastUpperWall = new(928f, 160f, 32f, 80f);
    private static readonly RectangleBounds DefaultVaultEastLowerWall = new(928f, 336f, 32f, 80f);
    private static readonly Vector2 DefaultHallLaserOrigin = new(336f, 417f);
    private static readonly Vector2 DefaultHallLaserDirection = Vector2.UnitY;
    private static readonly RectangleBounds DefaultCorridorGlassBounds = new(992f, 224f, 56f, 44f);
    private static readonly RectangleBounds DefaultLobbyGlassBounds = new(420f, 580f, 60f, 44f);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    public static LevelMapData LoadFromFile(string filePath, GameContext context)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return LoadFromJson(string.Empty, context);
        }

        string json = File.ReadAllText(filePath);
        return LoadFromJson(json, context);
    }

    public static LevelMapData LoadFromJson(string json, GameContext context)
    {
        LevelMapDto? dto = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<LevelMapDto>(json, SerializerOptions);

        LevelMapData mapData = ConvertDtoToMapData(dto);
        ApplyToContext(mapData, context);
        return mapData;
    }

    private static LevelMapData ConvertDtoToMapData(LevelMapDto? dto)
    {
        string levelName = string.IsNullOrWhiteSpace(dto?.LevelName) ? DefaultLevelName : dto.LevelName;
        int tileSize = dto?.TileSize > 0 ? dto.TileSize : DefaultTileSize;
        int gridWidth = dto?.Width > 0 ? dto.Width : DefaultGridWidth;
        int gridHeight = dto?.Height > 0 ? dto.Height : DefaultGridHeight;

        Vector2 playerSpawn = dto?.PlayerSpawn ?? DefaultPlayerSpawn;
        RectangleBounds getawayZone = ResolveGetawayZone(dto?.GetawayZone);
        List<RectangleBounds> walls = BuildWalls(dto?.Walls, gridWidth * tileSize, gridHeight * tileSize);
        List<MapPropObstacle> obstacles = ParseObstacles(dto?.Obstacles);
        List<MapInteractableDto> interactables = dto?.Interactables ?? new List<MapInteractableDto>();
        List<MapGuardDto> guards = dto?.Guards?.FindAll(g => g.Waypoints is { Count: > 0 }) ?? new List<MapGuardDto>();
        List<LaserSpawnData> lasers = BuildLasers(dto?.Lasers);
        List<GlassHazardSpawnData> glassHazards = BuildGlassHazards(dto?.GlassHazards);

        return new LevelMapData(
            levelName,
            tileSize,
            gridWidth,
            gridHeight,
            playerSpawn,
            getawayZone,
            walls,
            obstacles,
            interactables,
            guards,
            lasers,
            glassHazards);
    }

    private static RectangleBounds ResolveGetawayZone(RectangleBounds? zone) =>
        zone is { Width: > 0f, Height: > 0f } validZone ? validZone : DefaultGetawayZone;

    private static List<RectangleBounds> BuildWalls(
        List<RectangleBounds>? wallDtos,
        float mapPixelWidth,
        float mapPixelHeight)
    {
        List<RectangleBounds> customWalls = ParseCustomWalls(wallDtos);
        return customWalls.Count > 0 ? customWalls : BuildDefaultBankWalls(mapPixelWidth, mapPixelHeight);
    }

    private static List<RectangleBounds> ParseCustomWalls(List<RectangleBounds>? wallDtos) =>
        wallDtos is null ? new List<RectangleBounds>() : wallDtos.FindAll(wall => wall.Width > 0f && wall.Height > 0f);

    private static List<RectangleBounds> BuildDefaultBankWalls(float mapPixelWidth, float mapPixelHeight) =>
        new()
        {
            new(0f, 0f, mapPixelWidth, OuterWallThickness),
            new(0f, mapPixelHeight - OuterWallThickness, mapPixelWidth, OuterWallThickness),
            new(0f, 0f, OuterWallThickness, mapPixelHeight),
            new(mapPixelWidth - OuterWallThickness, 0f, OuterWallThickness, mapPixelHeight),
            DefaultVaultTopWall,
            DefaultVaultBottomWall,
            DefaultLobbyDividerWall,
            DefaultVaultEastUpperWall,
            DefaultVaultEastLowerWall
        };

    private static List<MapPropObstacle> ParseObstacles(List<MapPropObstacle>? dtos) =>
        dtos is null ? new List<MapPropObstacle>() : dtos.FindAll(o => o.Width > 0f && o.Height > 0f);

    private static List<LaserSpawnData> BuildLasers(List<LaserSpawnDto>? laserDtos)
    {
        var lasers = new List<LaserSpawnData>();

        if (laserDtos is not null)
        {
            foreach (LaserSpawnDto dto in laserDtos)
            {
                if (!string.IsNullOrWhiteSpace(dto.Id))
                {
                    Vector2 direction = dto.Direction ?? DefaultHallLaserDirection;
                    lasers.Add(new LaserSpawnData(dto.Id, new Vector2(dto.X, dto.Y), direction));
                }
            }
        }

        return lasers.Count > 0
            ? lasers
            : new List<LaserSpawnData> { new(DefaultHallLaserId, DefaultHallLaserOrigin, DefaultHallLaserDirection) };
    }

    private static List<GlassHazardSpawnData> BuildGlassHazards(List<GlassHazardDto>? glassDtos)
    {
        var hazards = new List<GlassHazardSpawnData>();

        if (glassDtos is not null)
        {
            foreach (GlassHazardDto dto in glassDtos)
            {
                if (!string.IsNullOrWhiteSpace(dto.Id) && dto.Width > 0f && dto.Height > 0f)
                {
                    hazards.Add(new GlassHazardSpawnData(dto.Id, new RectangleBounds(dto.X, dto.Y, dto.Width, dto.Height)));
                }
            }
        }

        return hazards.Count > 0
            ? hazards
            : new List<GlassHazardSpawnData>
            {
                new(DefaultCorridorGlassId, DefaultCorridorGlassBounds),
                new(DefaultLobbyGlassId, DefaultLobbyGlassBounds)
            };
    }

    private static void ApplyToContext(LevelMapData mapData, GameContext context)
    {
        context.PlayerPosition = mapData.PlayerSpawn;
        context.Walls.Clear();
        context.Walls.AddRange(mapData.Walls);
        if (mapData.Obstacles is not null)
        {
            foreach (MapPropObstacle obstacle in mapData.Obstacles)
            {
                context.Walls.Add(obstacle.ToBounds());
            }
        }
    }
}
