using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Orchestrates all 3D cel-shaded scene sub-renderers without mutating gameplay state.
/// </summary>
public sealed class Scene3DRenderer : IDisposable
{
    private readonly ToonModelLibrary _library = new();
    private readonly FloorWallsFurnitureRenderer _environment = new();
    private readonly BankDecorRenderer _decor = new();
    private readonly PropsRenderer _props = new();
    private readonly CharacterRenderer _characters = new();
    private readonly StealthVfxRenderer _vfx = new();
    private float _elapsedSeconds;
    private bool _assetsLoaded;

    public void Load(GameContext context)
    {
        if (!_assetsLoaded)
        {
            _library.LoadShader();
            _environment.Load(_library);
            _decor.Load(_library);
            _props.Load(_library);
            _characters.Load(_library);
            _assetsLoaded = true;
        }

        _vfx.SubscribeEvents(context.EventBus);
    }

    public void Update(float deltaTime, GameContext context, IReadOnlyList<Guard> guards, Vector3 cameraPosition)
    {
        _elapsedSeconds += deltaTime;
        _library.UpdateFrameUniforms(cameraPosition, context, guards, _elapsedSeconds);
        _props.Update(deltaTime, context);
        _characters.Update(deltaTime, context, guards);
        _vfx.Update(deltaTime, context);
    }

    public void Draw(
        GameContext context,
        IReadOnlyList<RectangleBounds> architecturalWalls,
        IReadOnlyList<RectangleBounds> visionBlockers,
        IReadOnlyList<Guard> guards,
        float playerRotationDegrees)
    {
        _environment.Draw(architecturalWalls, _elapsedSeconds);
        _decor.Draw(_elapsedSeconds);
        _vfx.Draw(visionBlockers, context, guards, _elapsedSeconds);
        _props.Draw(context, _elapsedSeconds);
        _characters.Draw(context, guards, playerRotationDegrees, _elapsedSeconds);
    }

    public void Dispose() => _library.Dispose();
}
