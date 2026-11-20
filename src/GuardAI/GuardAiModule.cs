using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.Rendering;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Owned by: Hatem (ShadowHeist.GuardAI)
/// Spawns guards and security cameras, handles acoustic/alarm investigation via A* pathfinding, and renders 3D guards/cameras.
/// </summary>
public sealed class GuardAiModule : IGameSystem, IDisposable
{
    private const float AlphaGuardSpeed = 90f;
    private const float BetaGuardSpeed = 78f;
    private const float MapGuardSpeedMultiplier = 1.15f;
    private const float DebugClickNoiseRadius = 400f;
    private const float LobbyWestCameraAngleDegrees = 180f;
    private const float CorridorSouthCameraAngleDegrees = 90f;

    private static readonly Vector2 LobbyWestCameraPos = new(314f, 224f);
    private static readonly Vector2 CorridorSouthCameraPos = new(640f, 422f);

    private static readonly Vector2[] PatrolRouteAlpha =
    {
        new(320f, 200f),
        new(860f, 200f),
        new(860f, 380f),
        new(320f, 380f)
    };

    private static readonly Vector2[] PatrolRouteBeta =
    {
        new(980f, 160f),
        new(980f, 540f)
    };

    private readonly List<Guard> _guards = new();
    private readonly List<SecurityCamera> _cameras = new();
    private readonly WorldLightingModule? _world;
    private GuardRenderer? _renderer;

    public IReadOnlyList<Guard> Guards => _guards;
    public IReadOnlyList<SecurityCamera> Cameras => _cameras;

    public GuardAiModule(WorldLightingModule? world = null)
    {
        _world = world;
    }

    public void Init(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _guards.Clear();
        _cameras.Clear();
        _renderer ??= new GuardRenderer();
        _renderer.Load(context.Models);
        SpawnInitialGuards();
        SpawnInitialCameras();

        context.EventBus.Subscribe<NoiseEmittedEvent>(evt => HandleNoiseEmitted(evt, context.Walls));
        context.EventBus.Subscribe<AlarmTriggeredEvent>(evt => HandleAlarmTriggered(evt, context.Walls));
        context.EventBus.Subscribe<PowerStateChangedEvent>(HandlePowerStateChanged);
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (context.Outcome != HeistOutcome.Playing)
        {
            return;
        }

        PublishDebugClickNoiseIfTriggered(context);
        IReadOnlyList<RectangleBounds> visionBlockers = _world?.GetVisionBlockingWalls(context) ?? context.Walls;

        foreach (SecurityCamera camera in _cameras)
        {
            camera.Update(deltaTime, context, visionBlockers);
        }

        foreach (Guard guard in _guards)
        {
            guard.Update(deltaTime, context);
        }

        _renderer?.Update(deltaTime, _guards, context.Models);
    }

    public void Draw(GameContext context)
    {
        IReadOnlyList<RectangleBounds> visionBlockers = _world?.GetVisionBlockingWalls(context) ?? context.Walls;
        SecurityCamera3DRenderer.Draw(_cameras, visionBlockers, context.IsAlarmTriggered, context.ElapsedSeconds);
        _renderer?.Draw(_guards, visionBlockers, context);
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
        _guards.Clear();
        _cameras.Clear();
    }

    // TODO (Hatem - Week 3 / Before Final Release): Remove debug left-click noise once CoinGadget (Ticket #20) is integrated.
    private static void PublishDebugClickNoiseIfTriggered(GameContext context)
    {
        if (!Raylib.IsWindowReady() || !Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            return;
        }

        Vector2 clickWorldPosition = context.AimWorldOverride
            ?? context.Camera.ScreenToGroundPoint(Raylib.GetMousePosition())
            ?? Raylib.GetMousePosition();

        context.EventBus.Publish(new NoiseEmittedEvent(clickWorldPosition, DebugClickNoiseRadius, "DebugClickNoise"));
    }

    private void HandleNoiseEmitted(NoiseEmittedEvent noiseEvent, IReadOnlyList<RectangleBounds> walls)
    {
        _renderer?.RecordNoisePulse(noiseEvent.Origin, noiseEvent.Radius);

        foreach (Guard guard in _guards)
        {
            guard.OnNoiseEmitted(noiseEvent, walls);
        }
    }

    private void HandleAlarmTriggered(AlarmTriggeredEvent alarmEvent, IReadOnlyList<RectangleBounds> walls)
    {
        bool isSensorAlarm = alarmEvent.Reason.StartsWith("LaserTripwire:", StringComparison.Ordinal) ||
                             alarmEvent.Reason.StartsWith("SecurityCamera:", StringComparison.Ordinal);
        if (!isSensorAlarm)
        {
            return;
        }

        foreach (Guard guard in _guards)
        {
            guard.NotifyNoise(alarmEvent.TriggerPosition, walls);
        }
    }

    private void HandlePowerStateChanged(PowerStateChangedEvent powerEvent)
    {
        foreach (SecurityCamera camera in _cameras)
        {
            camera.ApplyPowerState(powerEvent);
        }
    }

    private void SpawnInitialGuards()
    {
        IReadOnlyList<MapGuardDto>? mapGuards = _world?.CurrentMap?.Guards;
        if (mapGuards is { Count: > 0 })
        {
            foreach (MapGuardDto guardDto in mapGuards)
            {
                float tunedSpeed = guardDto.MoveSpeed * MapGuardSpeedMultiplier;
                _guards.Add(new Guard(guardDto.Name, guardDto.Waypoints[0], guardDto.Waypoints, tunedSpeed));
            }

            return;
        }

        _guards.Add(new Guard("guard_vault_patrol_alpha", PatrolRouteAlpha[0], PatrolRouteAlpha, moveSpeed: AlphaGuardSpeed));
        _guards.Add(new Guard("guard_corridor_patrol_beta", PatrolRouteBeta[0], PatrolRouteBeta, moveSpeed: BetaGuardSpeed));
    }

    private void SpawnInitialCameras()
    {
        _cameras.Add(new SecurityCamera("cam_lobby_west", LobbyWestCameraPos, LobbyWestCameraAngleDegrees));
        _cameras.Add(new SecurityCamera("cam_corridor_south", CorridorSouthCameraPos, CorridorSouthCameraAngleDegrees));
    }
}
