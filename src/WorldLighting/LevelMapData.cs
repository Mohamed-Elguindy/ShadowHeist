using System.Numerics;
using ShadowHeist.Core;

namespace ShadowHeist.WorldLighting;

public sealed record LevelMapData(
    string LevelName,
    int TileSize,
    int GridWidth,
    int GridHeight,
    Vector2 PlayerSpawn,
    RectangleBounds GetawayZone,
    IReadOnlyList<RectangleBounds> Walls,
    IReadOnlyList<InteractableSpawnData> Interactables,
    IReadOnlyList<GuardSpawnData> Guards);

public sealed record InteractableSpawnData(
    string Type,
    string Id,
    Vector2 Position,
    string? TargetId,
    int CashValue);

public sealed record GuardSpawnData(
    string Name,
    IReadOnlyList<Vector2> Waypoints);

public sealed class LevelMapDto
{
    public string? LevelName { get; set; }
    public int TileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public MapPointDto? PlayerSpawn { get; set; }
    public MapRectDto? GetawayZone { get; set; }
    public List<MapRectDto>? Walls { get; set; }
    public List<InteractableSpawnDto>? Interactables { get; set; }
    public List<GuardSpawnDto>? Guards { get; set; }
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

public sealed class InteractableSpawnDto
{
    public string? Type { get; set; }
    public string? Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public string? TargetId { get; set; }
    public int CashValue { get; set; }
}

public sealed class GuardSpawnDto
{
    public string? Name { get; set; }
    public List<MapPointDto>? Waypoints { get; set; }
}
