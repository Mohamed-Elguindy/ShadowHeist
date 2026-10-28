using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem.Stances;

/// <summary>
/// Sneak / crouch stance (Ticket #11 - Mahmoud).
/// Halved movement speed, completely silent (0 noise).
/// </summary>
public sealed class SneakStance : IPlayerStance
{
    private const float Speed = 0.55f;
    private const float Noise = 0f;

    public string Name => "Sneak";
    public float SpeedMultiplier => Speed;
    public float NoiseRadius => Noise;

    public IPlayerStance HandleInput(PlayerInputCommand command)
    {
        // Sprinting immediately breaks out of sneak
        if (command.IsSprintHeld)
        {
            return new SprintStance();
        }

        // Pressing Ctrl again toggles back to normal walk
        if (command.IsSneakToggled)
        {
            return new WalkStance();
        }

        return this;
    }
}
