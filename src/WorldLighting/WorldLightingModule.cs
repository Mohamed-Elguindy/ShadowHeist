using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.WorldLighting.Rendering;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Owned by: Ali (ShadowHeist.WorldLighting)
/// Loads level geometry, obstacles, and laser tripwires into GameContext and renders 2D lighting when 3D view is off.
/// </summary>
public sealed class WorldLightingModule : IGameSystem, IDisposable
{
    private const string Level1MapPath = "assets/maps/level1_city_bank.json";
    private const float AimDistanceEpsilonSquared = 0.001f;

    private readonly MapRenderer _mapRenderer = new();
    private readonly LightingEffectRenderer _lightingRenderer = new();
    private readonly List<LaserTripwire> _lasers = new();
    private IReadOnlyList<Vector2> _visionConeVertices = Array.Empty<Vector2>();
    private LevelMapData? _currentMap;
    private float _currentAimDegrees;
    private bool _texturesLoaded;

    public LevelMapData? CurrentMap => _currentMap;
    public IReadOnlyList<LaserTripwire> Lasers => _lasers;
    public IReadOnlyList<Vector2> PlayerVisionCone => _visionConeVertices;

    public void Init(GameContext context)
    {
        _currentMap = MapLoader.LoadFromFile(Level1MapPath, context);
        if (!_texturesLoaded)
        {
            _mapRenderer.LoadTextures();
            _lightingRenderer.LoadTextures();
            _texturesLoaded = true;
        }

        SpawnInitialLasers();
        _currentAimDegrees = context.PlayerRotationDegrees;
        _visionConeVertices = VisionRaycaster.ComputeVisionCone(
            context.PlayerPosition,
            _currentAimDegrees,
            _currentMap?.Walls ?? context.Walls);
        UpdateLasers(context);
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (context.Outcome != HeistOutcome.Playing) return;

        Vector2 aimTarget = context.AimWorldOverride ?? context.CurrentInput.AimWorldPosition;
        UpdateAimAngle(context.PlayerPosition, aimTarget, context.PlayerRotationDegrees);

        // Include closed doors from context.Walls while excluding waist-high furniture obstacles.
        IReadOnlyList<RectangleBounds> visionWalls = BuildVisionWalls(context);
        _visionConeVertices = VisionRaycaster.ComputeVisionCone(
            context.PlayerPosition,
            _currentAimDegrees,
            visionWalls);

        UpdateLasers(context, visionWalls);
    }

    public void Draw(GameContext context)
    {
        if (!context.Is3DRenderingActive)
        {
            _mapRenderer.Draw(_currentMap, context.Walls);
            _lightingRenderer.Draw(_visionConeVertices, _lasers);
        }
    }

    public void Dispose()
    {
        _mapRenderer.Dispose();
        _lightingRenderer.Dispose();
        _texturesLoaded = false;
    }

    private void SpawnInitialLasers()
    {
        _lasers.Clear();
        if (_currentMap?.Lasers is null)
        {
            return;
        }

        foreach (LaserSpawnData spawn in _currentMap.Lasers)
        {
            _lasers.Add(new LaserTripwire(spawn.Id, spawn.Origin, spawn.Direction));
        }
    }

    private void UpdateAimAngle(Vector2 playerPosition, Vector2 aimWorldPosition, float fallbackDegrees)
    {
        Vector2 toTarget = aimWorldPosition - playerPosition;
        if (toTarget.LengthSquared() < AimDistanceEpsilonSquared)
        {
            _currentAimDegrees = fallbackDegrees;
            return;
        }

        _currentAimDegrees = MathF.Atan2(toTarget.Y, toTarget.X) * HeistConstants.RadToDeg;
    }

    private void UpdateLasers(GameContext context, IReadOnlyList<RectangleBounds>? visionWalls = null)
    {
        IReadOnlyList<RectangleBounds> walls = visionWalls ?? BuildVisionWalls(context);
        foreach (LaserTripwire laser in _lasers)
        {
            laser.Update(context, walls: walls);
        }
    }

    private IReadOnlyList<RectangleBounds> BuildVisionWalls(GameContext context)
    {
        if (_currentMap?.Obstacles is not { Count: > 0 } obstacles)
        {
            return context.Walls;
        }

        var obstacleSet = obstacles.Select(o => o.ToBounds()).ToHashSet();
        var filtered = new List<RectangleBounds>(context.Walls.Count);
        foreach (RectangleBounds wall in context.Walls)
        {
            if (!obstacleSet.Contains(wall))
            {
                filtered.Add(wall);
            }
        }

        return filtered;
    }
}
