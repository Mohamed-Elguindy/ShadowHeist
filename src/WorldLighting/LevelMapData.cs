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
    IReadOnlyList<MapPropObstacle>? Obstacles = null,
    IReadOnlyList<MapInteractableDto>? Interactables = null,
    IReadOnlyList<MapGuardDto>? Guards = null,
    IReadOnlyList<LaserSpawnData>? Lasers = null,
    IReadOnlyList<GlassHazardSpawnData>? GlassHazards = null);

public readonly record struct MapPropObstacle(
    string Kind,
    float X,
    float Y,
    float Width,
    float Height)
{
    public RectangleBounds ToBounds() => new(X, Y, Width, Height);
}

public sealed record MapInteractableDto(
    string Type,
    string Id,
    float X,
    float Y,
    string? TargetId = null,
    int CashValue = 250_000);

public sealed record MapGuardDto(
    string Name,
    IReadOnlyList<Vector2> Waypoints,
    float MoveSpeed = 70f);

public sealed record LaserSpawnData(
    string Id,
    Vector2 Origin,
    Vector2 Direction);

public sealed record GlassHazardSpawnData(
    string Id,
    RectangleBounds Bounds);

public sealed class LevelMapDto
{
    public string? LevelName { get; set; }
    public int TileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Vector2? PlayerSpawn { get; set; }
    public RectangleBounds? GetawayZone { get; set; }
    public List<RectangleBounds>? Walls { get; set; }
    public List<MapPropObstacle>? Obstacles { get; set; }
    public List<MapInteractableDto>? Interactables { get; set; }
    public List<MapGuardDto>? Guards { get; set; }
    public List<LaserSpawnDto>? Lasers { get; set; }
    public List<GlassHazardDto>? GlassHazards { get; set; }
}

public sealed class LaserSpawnDto
{
    public string? Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public Vector2? Direction { get; set; }
}

public sealed class GlassHazardDto
{
    public string? Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}
