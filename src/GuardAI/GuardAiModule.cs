using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.GuardAI;

/// <summary>
/// Owned by: Hatem (ShadowHeist.GuardAI)
/// Week 1: Ticket #5 (Guard + PatrolState) &amp; Ticket #6 (Smooth Guard Rotation)
/// Week 2: Ticket #13 (SuspiciousState via EventBus) &amp; Ticket #14 (AStarPathfinder)
/// Week 3: Ticket #21 (AlertChaseState) &amp; Ticket #22 (Progressive Detection Meter)
/// Week 4: Ticket #29 (SecurityCamera) &amp; Ticket #30 (xUnit AI &amp; A* Unit Tests)
/// </summary>
public sealed class GuardAiModule : IGameSystem
{
    public void Init(GameContext context)
    {
        // TODO (Ticket #5 & #13 - Hatem): Initialize Guard entities and subscribe to NoiseEmittedEvent
    }

    public void Update(float deltaTime, GameContext context)
    {
        // TODO (Ticket #5 - Hatem): Update Guard active IGuardState
    }

    public void Draw(GameContext context)
    {
        // TODO (Ticket #5 - Hatem): Draw Guard sprites ("assets/sprites/guard_patrol.png")
    }
}
