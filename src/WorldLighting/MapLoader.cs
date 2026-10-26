using System.Numerics;
using System.Text.Json;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting;

public static class MapLoader
{
    private const string DefaultLevelName = "Level 1: City Central Bank";
    private const int DefaultTileSize = 64;
    private const int DefaultGridWidth = 20;
    private const int DefaultGridHeight = 12;
    private const float DefaultSpawnX = 128f;
    private const float DefaultSpawnY = 640f;

    private const float DefaultGetawayX = 64f;
    private const float DefaultGetawayY = 640f;
    private const float DefaultGetawayWidth = 128f;
    private const float DefaultGetawayHeight = 96f;

    private const float OuterWallThickness = 32f;
    private const float InteriorWallThickness = 32f;
    private const float VaultTopWallY = 160f;
    private const float VaultWallStartX = 320f;
    private const float VaultWallWidth = 640f;
    private const float LobbyDividerX = 320f;
    private const float LobbyDividerY = 160f;
    private const float LobbyDividerHeight = 224f;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
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

        Vector2 playerSpawn = ResolvePlayerSpawn(dto?.PlayerSpawn);
        RectangleBounds getawayZone = ResolveGetawayZone(dto?.GetawayZone);
        List<RectangleBounds> walls = BuildWalls(dto?.Walls, gridWidth * tileSize, gridHeight * tileSize);
        List<InteractableSpawnData> interactables = BuildInteractables(dto?.Interactables);
        List<GuardSpawnData> guards = BuildGuards(dto?.Guards);

        return new LevelMapData(
            levelName,
            tileSize,
            gridWidth,
            gridHeight,
            playerSpawn,
            getawayZone,
            walls,
            interactables,
            guards);
    }

    private static Vector2 ResolvePlayerSpawn(MapPointDto? spawnDto)
    {
        return spawnDto is not null
            ? new Vector2(spawnDto.X, spawnDto.Y)
            : new Vector2(DefaultSpawnX, DefaultSpawnY);
    }

    private static RectangleBounds ResolveGetawayZone(MapRectDto? zoneDto)
    {
        return zoneDto is { Width: > 0f, Height: > 0f }
            ? new RectangleBounds(zoneDto.X, zoneDto.Y, zoneDto.Width, zoneDto.Height)
            : new RectangleBounds(DefaultGetawayX, DefaultGetawayY, DefaultGetawayWidth, DefaultGetawayHeight);
    }

    private static List<RectangleBounds> BuildWalls(
        List<MapRectDto>? wallDtos,
        float mapPixelWidth,
        float mapPixelHeight)
    {
        var customWalls = ParseCustomWalls(wallDtos);
        if (customWalls.Count > 0)
        {
            return customWalls;
        }

        return BuildDefaultBankWalls(mapPixelWidth, mapPixelHeight);
    }

    private static List<RectangleBounds> ParseCustomWalls(List<MapRectDto>? wallDtos)
    {
        var walls = new List<RectangleBounds>();
        if (wallDtos is null)
        {
            return walls;
        }

        foreach (var wall in wallDtos)
        {
            if (wall.Width > 0f && wall.Height > 0f)
            {
                walls.Add(new RectangleBounds(wall.X, wall.Y, wall.Width, wall.Height));
            }
        }

        return walls;
    }

    private static List<RectangleBounds> BuildDefaultBankWalls(float mapPixelWidth, float mapPixelHeight)
    {
        return new List<RectangleBounds>
        {
            new(0f, 0f, mapPixelWidth, OuterWallThickness),
            new(0f, mapPixelHeight - OuterWallThickness, mapPixelWidth, OuterWallThickness),
            new(0f, 0f, OuterWallThickness, mapPixelHeight),
            new(mapPixelWidth - OuterWallThickness, 0f, OuterWallThickness, mapPixelHeight),
            new(VaultWallStartX, VaultTopWallY, VaultWallWidth, InteriorWallThickness),
            new(LobbyDividerX, LobbyDividerY, InteriorWallThickness, LobbyDividerHeight)
        };
    }

    private static List<InteractableSpawnData> BuildInteractables(List<InteractableSpawnDto>? interactableDtos)
    {
        var interactables = new List<InteractableSpawnData>();
        if (interactableDtos is null)
        {
            return interactables;
        }

        foreach (var item in interactableDtos)
        {
            if (string.IsNullOrWhiteSpace(item.Type) || string.IsNullOrWhiteSpace(item.Id))
            {
                continue;
            }

            interactables.Add(new InteractableSpawnData(
                item.Type,
                item.Id,
                new Vector2(item.X, item.Y),
                item.TargetId,
                item.CashValue));
        }

        return interactables;
    }

    private static List<GuardSpawnData> BuildGuards(List<GuardSpawnDto>? guardDtos)
    {
        var guards = new List<GuardSpawnData>();
        if (guardDtos is null)
        {
            return guards;
        }

        foreach (var guard in guardDtos)
        {
            if (string.IsNullOrWhiteSpace(guard.Name))
            {
                continue;
            }

            List<Vector2> waypoints = ParseWaypoints(guard.Waypoints);
            guards.Add(new GuardSpawnData(guard.Name, waypoints));
        }

        return guards;
    }

    private static List<Vector2> ParseWaypoints(List<MapPointDto>? waypointDtos)
    {
        var waypoints = new List<Vector2>();
        if (waypointDtos is null)
        {
            return waypoints;
        }

        foreach (var point in waypointDtos)
        {
            waypoints.Add(new Vector2(point.X, point.Y));
        }

        return waypoints;
    }

    private static void ApplyToContext(LevelMapData mapData, GameContext context)
    {
        context.PlayerPosition = mapData.PlayerSpawn;
        context.Walls.Clear();
        context.Walls.AddRange(mapData.Walls);
    }
}
