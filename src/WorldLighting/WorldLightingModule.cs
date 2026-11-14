using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.WorldLighting.Rendering;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Owned by: Ali (ShadowHeist.WorldLighting)
/// Week 1: Ticket #7 (JSON MapLoader) &amp; Ticket #8 (CollisionHelper Wall Sliding)
/// Week 2: Ticket #15 (VisionRaycaster Cone) &amp; Ticket #16 (LaserTripwire)
/// Week 3: Ticket #23 (ShadowRenderer RenderTexture2D) &amp; Ticket #24 (BrokenGlassTile Hazard)
/// Week 4: Ticket #31 (Level 1 &amp; 2 JSON Maps) &amp; Ticket #32 (Bloom Shader + AudioManager)
/// </summary>
public sealed class WorldLightingModule : IGameSystem, IDisposable
{
    private const string Level1MapPath = "assets/maps/level1_city_bank.json";
    private const float RadToDeg = 180f / MathF.PI;
    private const float AimDistanceEpsilonSquared = 0.001f;

    private readonly MapRenderer _mapRenderer = new();
    private readonly LightingEffectRenderer _lightingRenderer = new();
    private readonly List<LaserTripwire> _lasers = new();
    private IReadOnlyList<Vector2> _visionConeVertices = Array.Empty<Vector2>();
    private LevelMapData? _currentMap;
    private float _currentAimDegrees;

    public void Init(GameContext context)
    {
        _currentMap = MapLoader.LoadFromFile(Level1MapPath, context);
        _mapRenderer.LoadTextures();
        _lightingRenderer.LoadTextures();
        SpawnInitialLasers();
    }

    public void Update(float deltaTime, GameContext context)
    {
        context.PlayerPosition = CollisionHelper.ResolveWallCollision(
            context.PlayerPosition,
            CollisionHelper.DefaultActorRadius,
            context.Walls);

        UpdateAimAngle(context.PlayerPosition, context.CurrentInput.AimWorldPosition);
        _visionConeVertices = VisionRaycaster.ComputeVisionCone(
            context.PlayerPosition,
            _currentAimDegrees,
            context.Walls);

        UpdateLasers(context);
    }

    public void Draw(GameContext context)
    {
        _mapRenderer.Draw(_currentMap, context.Walls);
        _lightingRenderer.Draw(_visionConeVertices, _lasers);

        // TODO (Ticket #23 & #24 - Ali): Draw ShadowRenderer darkness mask and BrokenGlassTile hazards
    }

    public void Dispose()
    {
        _mapRenderer.Dispose();
        _lightingRenderer.Dispose();
    }

    private void SpawnInitialLasers()
    {
        _lasers.Clear();
        if (_currentMap is null)
        {
            return;
        }

        foreach (var spawn in _currentMap.Lasers)
        {
            _lasers.Add(new LaserTripwire(spawn.Id, spawn.Origin, spawn.Direction));
        }
    }

    private void UpdateAimAngle(Vector2 playerPosition, Vector2 aimWorldPosition)
    {
        Vector2 toTarget = aimWorldPosition - playerPosition;
        if (toTarget.LengthSquared() < AimDistanceEpsilonSquared)
        {
            return;
        }

        _currentAimDegrees = MathF.Atan2(toTarget.Y, toTarget.X) * RadToDeg;
    }

    private void UpdateLasers(GameContext context)
    {
        foreach (var laser in _lasers)
        {
            laser.Update(context);
        }
    }
}
