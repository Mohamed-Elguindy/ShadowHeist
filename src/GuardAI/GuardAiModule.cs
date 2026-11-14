using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.Rendering;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Owned by: Hatem (ShadowHeist.GuardAI)
/// Spawns guards from loaded map data, relays noise events, updates guard FSM states, and renders 3D guards and cones.
/// </summary>
public sealed class GuardAiModule : IGameSystem, IDisposable
{
    private const float HearingBonusRange = 55f;

    private readonly List<Guard> _guards = new();
    private readonly WorldLightingModule? _world;
    private GuardRenderer? _renderer;

    public IReadOnlyList<Guard> Guards => _guards;

    public GuardAiModule(WorldLightingModule? world = null)
    {
        _world = world;
    }

    public void Init(GameContext context)
    {
        _guards.Clear();
        _renderer ??= new GuardRenderer();
        _renderer.Load(context.Models);
        SpawnGuards();
        context.EventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
        context.EventBus.Subscribe<AlarmTriggeredEvent>(OnAlarmTriggered);
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (context.Outcome != HeistOutcome.Playing)
        {
            return;
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
        _renderer?.Draw(_guards, visionBlockers, context);
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
        _guards.Clear();
    }

    private void OnNoiseEmitted(NoiseEmittedEvent evt)
    {
        float maxDist = evt.Radius + HearingBonusRange;
        foreach (Guard guard in _guards)
        {
            if (Vector2.Distance(guard.Position, evt.Origin) <= maxDist)
            {
                guard.NotifyNoise(evt.Origin);
            }
        }
    }

    private void OnAlarmTriggered(AlarmTriggeredEvent evt)
    {
        if (!evt.Reason.StartsWith("LaserTripwire:", StringComparison.Ordinal))
        {
            return;
        }

        foreach (Guard guard in _guards)
        {
            guard.NotifyNoise(evt.TriggerPosition);
        }
    }

    private void SpawnGuards()
    {
        IReadOnlyList<MapGuardDto>? mapGuards = _world?.CurrentMap?.Guards;
        if (mapGuards is { Count: > 0 })
        {
            foreach (MapGuardDto dto in mapGuards)
            {
                _guards.Add(new Guard(dto.Name, dto.Waypoints[0], dto.Waypoints, dto.MoveSpeed));
            }

            return;
        }

        var vaultRoute = new List<Vector2> { new(410f, 228f), new(860f, 228f), new(860f, 348f), new(410f, 348f) };
        var lobbyRoute = new List<Vector2> { new(1060f, 104f), new(1060f, 480f), new(420f, 480f), new(1060f, 480f) };
        _guards.Add(new Guard("guard_vault_patrol_alpha", vaultRoute[0], vaultRoute, moveSpeed: 75f));
        _guards.Add(new Guard("guard_corridor_patrol_beta", lobbyRoute[0], lobbyRoute, moveSpeed: 65f));
    }
}
