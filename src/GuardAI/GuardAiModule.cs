using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.Rendering;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Owned by: Hatem (ShadowHeist.GuardAI)
/// Week 1: Ticket #5 (Guard + PatrolState) &amp; Ticket #6 (Smooth Guard Rotation)
/// Week 2: Ticket #13 (SuspiciousState via EventBus) &amp; Ticket #14 (AStarPathfinder)
/// </summary>
public sealed class GuardAiModule : IGameSystem, IDisposable
{
    private const float AlphaGuardSpeed = 75f;
    private const float BetaGuardSpeed = 65f;
    private const float DebugClickNoiseRadius = 400f;

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
    private GuardRenderer? _renderer;

    public IReadOnlyList<Guard> Guards => _guards;

    public void Init(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _guards.Clear();
        SpawnInitialGuards();

        context.EventBus.Subscribe<NoiseEmittedEvent>(evt => HandleNoiseEmitted(evt, context.Walls));
    }

    public void Update(float deltaTime, GameContext context)
    {
        if (_renderer is not null && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 clickPos = Raylib.GetMousePosition();
            context.EventBus.Publish(new NoiseEmittedEvent(clickPos, DebugClickNoiseRadius, "DebugClickNoise"));
        }

        foreach (var guard in _guards)
        {
            guard.Update(deltaTime, context);
        }
    }

    public void Draw(GameContext context)
    {
        _renderer ??= new GuardRenderer();
        _renderer.Draw(_guards);
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
        _guards.Clear();
    }

    private void HandleNoiseEmitted(NoiseEmittedEvent noiseEvent, IReadOnlyList<RectangleBounds> walls)
    {
        _renderer?.RecordNoisePulse(noiseEvent.Origin, noiseEvent.Radius);

        foreach (var guard in _guards)
        {
            guard.OnNoiseEmitted(noiseEvent, walls);
        }
    }

    private void SpawnInitialGuards()
    {
        var guardAlpha = new Guard("guard_vault_patrol_alpha", PatrolRouteAlpha[0], PatrolRouteAlpha, moveSpeed: AlphaGuardSpeed);
        var guardBeta = new Guard("guard_corridor_patrol_beta", PatrolRouteBeta[0], PatrolRouteBeta, moveSpeed: BetaGuardSpeed);

        _guards.Add(guardAlpha);
        _guards.Add(guardBeta);
    }
}
