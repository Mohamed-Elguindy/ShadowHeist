using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI.Rendering;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Owned by: Hatem (ShadowHeist.GuardAI)
/// Week 1: Ticket #5 (Guard + PatrolState) & Ticket #6 (Smooth Guard Rotation)
/// Week 2: Ticket #13 (SuspiciousState via EventBus) & Ticket #14 (AStarPathfinder)
/// </summary>
public sealed class GuardAiModule : IGameSystem, IDisposable
{
    private readonly List<Guard> _guards = new();
    private GuardRenderer? _renderer;

    public IReadOnlyList<Guard> Guards => _guards;

    public void Init(GameContext context)
    {
        _renderer = new GuardRenderer();
        SpawnInitialGuards();
    }

    public void Update(float deltaTime, GameContext context)
    {
        foreach (var guard in _guards)
        {
            guard.Update(deltaTime, context);
        }
    }

    public void Draw(GameContext context)
    {
        _renderer?.Draw(_guards);
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
        _guards.Clear();
    }

    private void SpawnInitialGuards()
    {
        var patrolRouteAlpha = new List<Vector2>
        {
            new(320f, 200f),
            new(860f, 200f),
            new(860f, 380f),
            new(320f, 380f)
        };

        var patrolRouteBeta = new List<Vector2>
        {
            new(980f, 160f),
            new(980f, 540f)
        };

        var guardAlpha = new Guard("guard_vault_patrol_alpha", patrolRouteAlpha[0], patrolRouteAlpha, moveSpeed: 75f);
        var guardBeta = new Guard("guard_corridor_patrol_beta", patrolRouteBeta[0], patrolRouteBeta, moveSpeed: 65f);

        _guards.Add(guardAlpha);
        _guards.Add(guardBeta);
    }
}
