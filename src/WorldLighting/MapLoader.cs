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
    private const float OuterWallThickness = 32f;

    private static readonly Vector2 DefaultPlayerSpawn = new(128f, 640f);
    private static readonly RectangleBounds DefaultGetawayZone = new(64f, 640f, 128f, 96f);
    private static readonly RectangleBounds DefaultVaultTopWall = new(320f, 160f, 640f, 32f);
    private static readonly RectangleBounds DefaultLobbyDividerWall = new(320f, 160f, 32f, 224f);

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

        return new LevelMapData(
            levelName,
            tileSize,
            gridWidth,
            gridHeight,
            playerSpawn,
            getawayZone,
            walls);
    }

    private static RectangleBounds ResolveGetawayZone(RectangleBounds? zone)
    {
        return zone is { Width: > 0f, Height: > 0f } validZone
            ? validZone
            : DefaultGetawayZone;
    }

    private static List<RectangleBounds> BuildWalls(
        List<RectangleBounds>? wallDtos,
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

    private static List<RectangleBounds> ParseCustomWalls(List<RectangleBounds>? wallDtos)
    {
        if (wallDtos is null)
        {
            return new List<RectangleBounds>();
        }

        return wallDtos.FindAll(wall => wall.Width > 0f && wall.Height > 0f);
    }

    private static List<RectangleBounds> BuildDefaultBankWalls(float mapPixelWidth, float mapPixelHeight)
    {
        return new List<RectangleBounds>
        {
            new(0f, 0f, mapPixelWidth, OuterWallThickness),
            new(0f, mapPixelHeight - OuterWallThickness, mapPixelWidth, OuterWallThickness),
            new(0f, 0f, OuterWallThickness, mapPixelHeight),
            new(mapPixelWidth - OuterWallThickness, 0f, OuterWallThickness, mapPixelHeight),
            DefaultVaultTopWall,
            DefaultLobbyDividerWall
        };
    }

    private static void ApplyToContext(LevelMapData mapData, GameContext context)
    {
        context.PlayerPosition = mapData.PlayerSpawn;
        context.Walls.Clear();
        context.Walls.AddRange(mapData.Walls);
    }
}
