using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// 3D tactical isometric presentation module (F5 toggles 2D/3D, F6 toggles Follow/Overview).
/// </summary>
public sealed class Viewport3DModule : IGameSystem, IDisposable
{
    private readonly GuardAiModule _guards;
    private readonly WorldLightingModule _world;
    private readonly Scene3DRenderer _scene = new();
    private Camera3D _camera;
    private Vector3 _cameraFocus;
    private Vector2? _lastAimPoint;
    private float _lastRotationDegrees;
    private bool _is3DMode = true;
    private bool _isOverviewCamera;
    private bool _hasFocus;
    private float _zoom = 1f;

    public Viewport3DModule(GuardAiModule guards, WorldLightingModule world)
    {
        _guards = guards;
        _world = world;
    }

    public void Init(GameContext context)
    {
        _scene.Load(context);
        _hasFocus = false;
        _camera = new Camera3D
        {
            Up = Vector3.UnitY,
            FovY = Viewport3DConfig.CameraFieldOfView,
            Projection = CameraProjection.Perspective
        };
        context.Is3DRenderingActive = _is3DMode;
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F5)) _is3DMode = !_is3DMode;
        if (Raylib.IsKeyPressed(KeyboardKey.F6)) _isOverviewCamera = !_isOverviewCamera;
        context.Is3DRenderingActive = _is3DMode;

        float wheel = Raylib.GetMouseWheelMove();
        if (MathF.Abs(wheel) > 0.01f)
        {
            _zoom = Math.Clamp(_zoom - wheel * Viewport3DConfig.ZoomStep, Viewport3DConfig.MinZoom, Viewport3DConfig.MaxZoom);
        }

        UpdateCamera(deltaTime, context);
        UpdateMouseAim(context);
        _scene.Update(deltaTime, context, _guards.Guards, _camera.Position);
    }

    public void Draw(GameContext context)
    {
        if (!_is3DMode)
        {
            HudOverlay3D.DrawHintBar("[F5] 3D VIEW   |   [R] RESTART");
            return;
        }

        Rlgl.DrawRenderBatchActive();
        Color sky = context.IsAlarmTriggered ? Viewport3DConfig.AlarmSkyColor : Viewport3DConfig.SkyColor;
        Raylib.ClearBackground(sky);

        IReadOnlyList<RectangleBounds> architecturalWalls = _world.CurrentMap?.Walls ?? context.Walls;
        IReadOnlyList<RectangleBounds> visionBlockers = BuildVisionBlockers(architecturalWalls, context);

        Raylib.BeginMode3D(_camera);
        _scene.Draw(context, architecturalWalls, visionBlockers, _guards.Guards, ReadPlayerRotation(context));
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

    private void UpdateMouseAim(GameContext context)
    {
        if (!_is3DMode)
        {
            context.AimWorldOverride = null;
            return;
        }

        _lastRotationDegrees = ReadPlayerRotation(context);
        context.AimWorldOverride = _lastAimPoint;
    }

    private float ReadPlayerRotation(GameContext context)
    {
        Ray ray = Raylib.GetMouseRay(Raylib.GetMousePosition(), _camera);
        if (ray.Direction.Y < -0.001f)
        {
            float hitDistance = -ray.Position.Y / ray.Direction.Y;
            if (hitDistance > 0f && hitDistance < 5000f)
            {
                Vector3 hit = ray.Position + ray.Direction * hitDistance;
                _lastAimPoint = new Vector2(hit.X, hit.Z);
            }
        }

        if (_lastAimPoint is not Vector2 aimPoint) return _lastRotationDegrees;
        Vector2 toCursor = aimPoint - context.PlayerPosition;
        if (toCursor.LengthSquared() < 1f) return _lastRotationDegrees;

        _lastRotationDegrees = MathF.Atan2(toCursor.Y, toCursor.X) * Raylib.RAD2DEG;
        return _lastRotationDegrees;
    }

    private static IReadOnlyList<RectangleBounds> BuildVisionBlockers(
        IReadOnlyList<RectangleBounds> architecturalWalls,
        GameContext context)
    {
        List<RectangleBounds>? blockers = null;
        foreach (VaultDoor door in context.Interactables.OfType<VaultDoor>())
        {
            if (door.IsOpen) continue;
            blockers ??= new List<RectangleBounds>(architecturalWalls);
            blockers.Add(door.DoorBounds);
        }

        return blockers ?? architecturalWalls;
    }
}
