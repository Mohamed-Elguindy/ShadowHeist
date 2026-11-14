using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld;
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
    private readonly HeistWorldModule? _heist;
    private readonly Scene3DRenderer _scene = new();
    private readonly TacticalCameraController _cameraController = new();

    private bool _is3DMode = true;

    public Viewport3DModule(GuardAiModule guards, WorldLightingModule world, HeistWorldModule? heist = null)
    {
        _guards = guards;
        _world = world;
        _heist = heist;
    }

    public void Init(GameContext context)
    {
        _scene.Load(context);
        context.Is3DRenderingActive = _is3DMode;
        _cameraController.Reset(context.PlayerPosition);
        SyncAimOverride(context);
    }

    public void SyncAimOverride(GameContext context)
    {
        if (!_is3DMode)
        {
            context.AimWorldOverride = null;
            return;
        }

        if (_cameraController.ScreenToGroundPoint(Raylib.GetMousePosition()) is Vector2 groundPoint)
        {
            context.AimWorldOverride = groundPoint;
        }
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F5)) _is3DMode = !_is3DMode;
        if (Raylib.IsKeyPressed(KeyboardKey.F6)) _cameraController.ToggleOverviewMode();
        context.Is3DRenderingActive = _is3DMode;

        _cameraController.ApplyZoomWheel(Raylib.GetMouseWheelMove());
        _cameraController.Update(deltaTime, context.PlayerPosition);
        SyncAimOverride(context);

        _scene.Update(deltaTime, context, _guards.Guards, _cameraController.Camera.Position);
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

        var frame = new Scene3DRenderFrame(
            context,
            architecturalWalls,
            visionBlockers,
            _guards.Guards,
            _world.Lasers,
            _world.CurrentMap?.GetawayZone,
            context.PlayerRotationDegrees,
            _scene.ElapsedSeconds);

        Raylib.BeginMode3D(_cameraController.Camera);
        _scene.Draw(in frame);
        Raylib.EndMode3D();

        HudOverlay3D.DrawFullHud(
            context,
            _cameraController.Camera,
            _cameraController.IsOverviewMode,
            _heist?.SoundMeterLevel ?? 0f);
    }

    public void Dispose() => _scene.Dispose();

    private static IReadOnlyList<RectangleBounds> BuildVisionBlockers(
        IReadOnlyList<RectangleBounds> architecturalWalls,
        GameContext context)
    {
        List<RectangleBounds>? blockers = null;
        foreach (VaultDoor door in context.Interactables.OfType<VaultDoor>())
        {
            if (door.IsOpen)
            {
                continue;
            }

            blockers ??= new List<RectangleBounds>(architecturalWalls);
            blockers.Add(door.DoorBounds);
        }

        return blockers ?? architecturalWalls;
    }
}
