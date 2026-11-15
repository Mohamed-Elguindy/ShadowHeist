namespace ShadowHeist.Core.Interfaces;

/// <summary>
/// Finite State Machine contract for Guard AI states: PatrolState, SuspiciousState, AlertChaseState.
/// </summary>
public interface IGuardState
{
    string StateName { get; }
    void Enter();
    IGuardState Update(float deltaTime, GameContext context);
}
