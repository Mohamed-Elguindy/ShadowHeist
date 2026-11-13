using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// 3D tactical isometric presentation module (F5 toggles 2D/3D, F6 toggles Follow/Overview).
/// Purely read-only observer of GameContext and GuardAiModule.
/// </summary>
public sealed class Viewport3DModule : IGameSystem, IDisposable
{
    private readonly GuardAiModule _guards;
    private readonly Scene3DRenderer _scene = new();
    private Camera3D _camera;
    private Vector3 _cameraFocus;
    private bool _is3DMode = true;
    private bool _isOverviewCamera;
    private bool _hasFocus;
    private float _zoom = 1f;

    public Viewport3DModule(GuardAiModule guards) => _guards = guards;

    public void Init(GameContext context)
    {
        _scene.Load(context);
        _camera = new Camera3D
        {
            Up = Vector3.UnitY,
            FovY = Viewport3DConfig.CameraFieldOfView,
            Projection = CameraProjection.Perspective
        };
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F5)) _is3DMode = !_is3DMode;
        if (Raylib.IsKeyPressed(KeyboardKey.F6)) _isOverviewCamera = !_isOverviewCamera;

        float wheel = Raylib.GetMouseWheelMove();
        if (MathF.Abs(wheel) > 0.01f)
        {
            _zoom = Math.Clamp(_zoom - wheel * Viewport3DConfig.ZoomStep, Viewport3DConfig.MinZoom, Viewport3DConfig.MaxZoom);
        }

        UpdateCamera(deltaTime, context);
        _scene.Update(deltaTime, context, _guards.Guards, _camera.Position);
    }

    public void Draw(GameContext context)
    {
        if (!_is3DMode)
        {
            HudOverlay3D.DrawHintBar("[F5] 3D VIEW");
            return;
        }

        // Flush earlier 2D systems before clearing color + depth buffers for the 3D pass.
        Rlgl.DrawRenderBatchActive();
        Color sky = context.IsAlarmTriggered ? Viewport3DConfig.AlarmSkyColor : Viewport3DConfig.SkyColor;
        Raylib.ClearBackground(sky);

        Raylib.BeginMode3D(_camera);
        _scene.Draw(context, _guards.Guards, ReadPlayerRotation(context));
        Raylib.EndMode3D();

        HudOverlay3D.DrawFullHud(context, _camera, _isOverviewCamera);
    }

    public void Dispose() => _scene.Dispose();

    private void UpdateCamera(float deltaTime, GameContext context)
    {
        Vector3 followTarget = Vector3.Lerp(
            new Vector3(context.PlayerPosition.X, 0f, context.PlayerPosition.Y),
            Viewport3DConfig.OverviewTarget,
            0.22f);

        Vector3 desiredTarget = _isOverviewCamera ? Viewport3DConfig.OverviewTarget : followTarget;
        Vector3 desiredOffset = (_isOverviewCamera ? Viewport3DConfig.OverviewCameraOffset : Viewport3DConfig.FollowCameraOffset) * _zoom;

        _cameraFocus = _hasFocus
            ? Vector3.Lerp(_cameraFocus, desiredTarget, MathF.Min(1f, Viewport3DConfig.CameraFollowSpeed * deltaTime))
            : desiredTarget;
        _hasFocus = true;

        _camera.FovY = _isOverviewCamera ? 62f : Viewport3DConfig.CameraFieldOfView;
        _camera.Target = _cameraFocus;
        _camera.Position = _cameraFocus + desiredOffset;
    }

    private float ReadPlayerRotation(GameContext context)
    {
        Ray ray = Raylib.GetMouseRay(Raylib.GetMousePosition(), _camera);
        if (MathF.Abs(ray.Direction.Y) < 0.0001f)
        {
            return 0f;
        }

        float hitDistance = -ray.Position.Y / ray.Direction.Y;
        Vector3 hit = ray.Position + ray.Direction * hitDistance;
        Vector2 toCursor = new Vector2(hit.X, hit.Z) - context.PlayerPosition;
        return toCursor == Vector2.Zero ? 0f : MathF.Atan2(toCursor.Y, toCursor.X) * Raylib.RAD2DEG;
    }
}
