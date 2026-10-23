namespace ShadowHeist.Core.Interfaces;

/// <summary>
/// Finite State Machine contract for Guard AI states: PatrolState, SuspiciousState, AlertChaseState (Tickets #5, #13, #21 — Hatem).
/// </summary>
public interface IGuardState
{
    string StateName { get; }
    void Enter();
    IGuardState Update(float deltaTime, GameContext context);
}
