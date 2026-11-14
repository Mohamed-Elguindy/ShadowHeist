using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Orchestrates modular ISceneRenderPass layers cleanly in draw order without mutating gameplay state.
/// </summary>
public sealed class Scene3DRenderer : IDisposable
{
    private readonly ToonModelLibrary _library = new();
    private readonly Architecture3DRenderer _architecture = new();
    private readonly Furniture3DRenderer _furniture = new();
    private readonly BankDecorRenderer _decor = new();
    private readonly PropsRenderer _props = new();
    private readonly CharacterRenderer _characters = new();
    private readonly StealthVfxRenderer _vfx = new();
    private readonly IReadOnlyList<ISceneRenderPass> _renderPasses;

    private float _elapsedSeconds;
    private bool _assetsLoaded;

    public float ElapsedSeconds => _elapsedSeconds;

    public Scene3DRenderer()
    {
        _renderPasses = new ISceneRenderPass[]
        {
            _architecture,
            _furniture,
            _decor,
            _vfx,
            new VisionCone3DRenderer(),
            new LaserTripwire3DRenderer(),
            _props,
            _characters
        };
    }

    public void Load(GameContext context)
    {
        if (!_assetsLoaded && Raylib.IsWindowReady())
        {
            _library.LoadShader();
            _architecture.Load(_library);
            _furniture.Load(_library);
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

    public void Draw(in Scene3DRenderFrame frame)
    {
        foreach (ISceneRenderPass pass in _renderPasses)
        {
            pass.Draw(frame);
        }
    }

    public void Dispose() => _library.Dispose();
}
