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
    IReadOnlyList<MapPropObstacle>? Obstacles = null);

public readonly record struct MapPropObstacle(
    string Kind,
    float X,
    float Y,
    float Width,
    float Height)
{
    public RectangleBounds ToBounds() => new(X, Y, Width, Height);
}

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
}
