using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

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

    private readonly MapRenderer _mapRenderer = new();
    private LevelMapData? _currentMap;

    public void Init(GameContext context)
    {
        _currentMap = MapLoader.LoadFromFile(Level1MapPath, context);
        _mapRenderer.LoadTextures();
    }

    public void Update(float deltaTime, GameContext context)
    {
        context.PlayerPosition = CollisionHelper.ResolveWallCollision(
            context.PlayerPosition,
            CollisionHelper.DefaultActorRadius,
            context.Walls);

        // TODO (Ticket #15 & #16 - Ali): Update VisionRaycaster and LaserTripwire beams
    }

    public void Draw(GameContext context)
    {
        _mapRenderer.Draw(_currentMap, context.Walls);
    }

    public void Dispose()
    {
        _mapRenderer.Dispose();
    }
}
