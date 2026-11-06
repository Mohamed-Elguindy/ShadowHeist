using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem.Stances;

/// <summary>
/// Normal walking stance (Ticket #11 - Mahmoud).
/// Standard speed and moderate noise footprint.
/// </summary>
public sealed class WalkStance : IPlayerStance
{
    private const float Speed = 1.0f;
    private const float Noise = 80f;

    public string Name => "Walk";
    public float SpeedMultiplier => Speed;
    public float NoiseRadius => Noise;

    public IPlayerStance HandleInput(PlayerInputCommand command)
    {
        // Holding Shift enters sprint
        if (command.IsSprintHeld)
        {
            return new SprintStance();
        }

        // Pressing Ctrl toggles sneak crouch
        if (command.IsSneakToggled)
        {
            return new SneakStance();
        }

        return this;
    }
}

