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
    IReadOnlyList<LaserSpawnData> Lasers);

public sealed record LaserSpawnData(
    string Id,
    Vector2 Origin,
    Vector2 Direction);

public sealed class LevelMapDto
{
    public string? LevelName { get; set; }
    public int TileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Vector2? PlayerSpawn { get; set; }
    public RectangleBounds? GetawayZone { get; set; }
    public List<RectangleBounds>? Walls { get; set; }
    public List<LaserSpawnDto>? Lasers { get; set; }
}

public sealed class LaserSpawnDto
{
    public string? Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public Vector2? Direction { get; set; }
}
