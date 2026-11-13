using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Owned by: Ali (ShadowHeist.WorldLighting)
/// Loads level geometry/obstacles into GameContext and renders 2D blueprint view when 3D view is off.
/// </summary>
public sealed class WorldLightingModule : IGameSystem, IDisposable
{
    private const string Level1MapPath = "assets/maps/level1_city_bank.json";

    private readonly MapRenderer _mapRenderer = new();
    private LevelMapData? _currentMap;
    private bool _texturesLoaded;

    public LevelMapData? CurrentMap => _currentMap;

    public void Init(GameContext context)
    {
        _currentMap = MapLoader.LoadFromFile(Level1MapPath, context);
        if (!_texturesLoaded)
        {
            _mapRenderer.LoadTextures();
            _texturesLoaded = true;
        }
    }

    public void Update(float deltaTime, GameContext context)
    {
        // Collision is resolved by PlayerSystemModule and GuardAiModule directly against context.Walls.
    }

    public void Draw(GameContext context)
    {
        if (!context.Is3DRenderingActive)
        {
            _mapRenderer.Draw(_currentMap, context.Walls);
        }
    }

    public void Dispose()
    {
        _mapRenderer.Dispose();
        _texturesLoaded = false;
    }
}
