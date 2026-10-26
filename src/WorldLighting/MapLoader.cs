using System.Numerics;
using System.Text.Json;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Data-driven JSON map loader and DTO definitions (Ticket #7 - Ali).
/// Deserializes level geometry, floor dimensions, and spawn coordinates into <see cref="GameContext"/>.
/// </summary>
public static class MapLoader
{
    private const string DefaultLevelName = "Level 1: City Central Bank";
    private const int DefaultTileSize = 64;
    private const int DefaultGridWidth = 20;
    private const int DefaultGridHeight = 12;
    private const float DefaultSpawnX = 128f;
    private const float DefaultSpawnY = 640f;

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
        ArgumentNullException.ThrowIfNull(context);

        LevelMapDto? dto = null;
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            dto = JsonSerializer.Deserialize<LevelMapDto>(json, SerializerOptions);
        }

        LevelMapData mapData = ConvertDtoToMapData(dto);
        ApplyToContext(mapData, context);
        return mapData;
    }

    public static LevelMapData LoadFromJson(string json, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var dto = string.IsNullOrWhiteSpace(json)
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
        int width = dto?.Width > 0 ? dto.Width : DefaultGridWidth;
        int height = dto?.Height > 0 ? dto.Height : DefaultGridHeight;

        Vector2 playerSpawn = dto?.PlayerSpawn is not null
            ? new Vector2(dto.PlayerSpawn.X, dto.PlayerSpawn.Y)
            : new Vector2(DefaultSpawnX, DefaultSpawnY);

        List<RectangleBounds> walls = BuildWalls(dto, width * tileSize, height * tileSize);

        return new LevelMapData(levelName, tileSize, width, height, playerSpawn, walls);
    }

    private static List<RectangleBounds> BuildWalls(LevelMapDto? dto, float mapPixelWidth, float mapPixelHeight)
    {
        var walls = new List<RectangleBounds>();

        if (dto?.Walls is { Count: > 0 })
        {
            foreach (var wall in dto.Walls)
            {
                walls.Add(new RectangleBounds(wall.X, wall.Y, wall.Width, wall.Height));
            }

            return walls;
        }

        // Outer bank perimeter walls
        walls.Add(new RectangleBounds(0f, 0f, mapPixelWidth, OuterWallThickness));
        walls.Add(new RectangleBounds(0f, mapPixelHeight - OuterWallThickness, mapPixelWidth, OuterWallThickness));
        walls.Add(new RectangleBounds(0f, 0f, OuterWallThickness, mapPixelHeight));
        walls.Add(new RectangleBounds(mapPixelWidth - OuterWallThickness, 0f, OuterWallThickness, mapPixelHeight));

        // Interior vault corridor partition walls
        walls.Add(new RectangleBounds(VaultWallStartX, VaultTopWallY, VaultWallWidth, InteriorWallThickness));
        walls.Add(new RectangleBounds(LobbyDividerX, LobbyDividerY, InteriorWallThickness, LobbyDividerHeight));

        return walls;
    }

    private static void ApplyToContext(LevelMapData mapData, GameContext context)
    {
        context.PlayerPosition = mapData.PlayerSpawn;
        context.Walls.Clear();
        context.Walls.AddRange(mapData.Walls);
    }
}

public sealed record LevelMapData(
    string LevelName,
    int TileSize,
    int GridWidth,
    int GridHeight,
    Vector2 PlayerSpawn,
    IReadOnlyList<RectangleBounds> Walls);

public sealed class LevelMapDto
{
    public string? LevelName { get; set; }
    public int TileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public MapPointDto? PlayerSpawn { get; set; }
    public MapRectDto? GetawayZone { get; set; }
    public List<MapRectDto>? Walls { get; set; }
}

public sealed class MapPointDto
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class MapRectDto
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}
