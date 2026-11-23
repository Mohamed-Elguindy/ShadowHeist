using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.WorldLighting.Rendering;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Owned by: Ali (ShadowHeist.WorldLighting)
/// Loads level geometry, obstacles, and laser tripwires into GameContext and renders the 3D bank environment, furniture, lasers, and player FOV.
/// </summary>
public sealed class WorldLightingModule : IGameSystem, IDisposable
{
    private const string Level1MapPath = "assets/maps/level1_city_bank.json";
    private const string AllPowerGroupId = "all";
    private const float AimDistanceEpsilonSquared = 0.001f;

    private readonly Architecture3DRenderer _architecture = new();
    private readonly Furniture3DRenderer _furniture = new();
    private readonly BankDecorRenderer _decor = new();
    private readonly LaserTripwire3DRenderer _laserRenderer = new();
    private readonly List<LaserTripwire> _lasers = new();
    private IReadOnlyList<Vector2> _visionConeVertices = Array.Empty<Vector2>();
    private IReadOnlyList<RectangleBounds>? _visionBlockingWalls;
    private LevelMapData? _currentMap;
    private float _currentAimDegrees;
    private bool _modelsLoaded;

    public LevelMapData? CurrentMap => _currentMap;
    public IReadOnlyList<LaserTripwire> Lasers => _lasers;
    public IReadOnlyList<Vector2> PlayerVisionCone => _visionConeVertices;

    public IReadOnlyList<RectangleBounds> GetVisionBlockingWalls(GameContext context) =>
        _visionBlockingWalls ?? BuildWallsExcludingWaistHighObstacles(context);

    public void Init(GameContext context)
    {
        string mapPath = File.Exists(context.SelectedMapPath) ? context.SelectedMapPath : Level1MapPath;
        _currentMap = MapLoader.LoadFromFile(mapPath, context);
        _visionBlockingWalls = BuildWallsExcludingWaistHighObstacles(context);
        EnsureModelsLoaded(context);
        SpawnInitialLasers();
        context.EventBus.Subscribe<PowerStateChangedEvent>(OnPowerStateChanged);

        _currentAimDegrees = context.PlayerRotationDegrees;
        _visionConeVertices = VisionRaycaster.ComputeVisionCone(
            context.PlayerPosition,
            _currentAimDegrees,
            _visionBlockingWalls);
        UpdateLasers(context, _visionBlockingWalls);
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (context.Outcome != HeistOutcome.Playing)
        {
            return;
        }

        Vector2 aimTarget = context.AimWorldOverride ?? context.CurrentInput.AimWorldPosition;
        UpdateAimAngle(context.PlayerPosition, aimTarget, context.PlayerRotationDegrees);

        IReadOnlyList<RectangleBounds> visionWalls = GetVisionBlockingWalls(context);
        _visionConeVertices = VisionRaycaster.ComputeVisionCone(
            context.PlayerPosition,
            _currentAimDegrees,
            visionWalls);

        UpdateLasers(context, visionWalls);
    }

    public void Draw(GameContext context)
    {
        ExteriorStreet3DRenderer.Draw(context.ElapsedSeconds, context.IsAlarmTriggered);
        _architecture.Draw(_currentMap?.Walls ?? context.Walls, _currentMap?.GetawayZone, context.ElapsedSeconds, context.PlayerPosition, context.Camera.Camera.Position);
        _furniture.Draw();
        _decor.Draw(context);
        if (context.AreLobbyLightsPowered)
        {
            VisionCone3DRenderer.DrawWarmLampGlows();
        }
        AmbientAtmosphere3DRenderer.Draw(context.ElapsedSeconds, context.AreLobbyLightsPowered);
        VisionCone3DRenderer.DrawPlayerVisionCone(_visionConeVertices);
        _laserRenderer.Draw(_lasers, context.ElapsedSeconds);
    }

    public void Dispose() => _modelsLoaded = false;

    private void OnPowerStateChanged(PowerStateChangedEvent evt)
    {
        foreach (LaserTripwire laser in _lasers)
        {
            if (MatchesPowerGroup(evt.TargetGroupId, laser.Id))
            {
                laser.IsActive = evt.IsPowered;
            }
        }
    }

    private static bool MatchesPowerGroup(string targetGroupId, string deviceId) =>
        string.Equals(targetGroupId, deviceId, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(targetGroupId, AllPowerGroupId, StringComparison.OrdinalIgnoreCase);

    private void EnsureModelsLoaded(GameContext context)
    {
        if (_modelsLoaded || !Raylib.IsWindowReady())
        {
            return;
        }

        context.Models.EnsureLoaded();
        _architecture.Load(context.Models);
        _furniture.Load(context.Models);
        _decor.Load(context.Models);
        _modelsLoaded = true;
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
        IReadOnlyList<RectangleBounds> walls = visionWalls ?? BuildWallsExcludingWaistHighObstacles(context);
        foreach (LaserTripwire laser in _lasers)
        {
            laser.Update(context, walls: walls);
        }
    }

    private IReadOnlyList<RectangleBounds> BuildWallsExcludingWaistHighObstacles(GameContext context)
    {
        if (_currentMap?.Obstacles is not { Count: > 0 } obstacles)
        {
            return context.Walls;
        }

        var obstacleSet = obstacles.Select(obstacle => obstacle.ToBounds()).ToHashSet();
        var filteredWalls = new List<RectangleBounds>(context.Walls.Count);
        foreach (RectangleBounds wall in context.Walls)
        {
            if (!obstacleSet.Contains(wall))
            {
                filteredWalls.Add(wall);
            }
        }

        return filteredWalls;
    }
}
