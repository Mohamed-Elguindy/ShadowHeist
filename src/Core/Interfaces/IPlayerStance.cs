namespace ShadowHeist.Core.Interfaces;

/// <summary>
/// State Pattern contract for Player movement stances: SneakStance, WalkStance, SprintStance (Ticket #11 — Mahmoud).
/// </summary>
public interface IPlayerStance
{
    string Name { get; }
    float SpeedMultiplier { get; }
    float NoiseRadius { get; }
    IPlayerStance HandleInput(PlayerInputCommand command);
}
