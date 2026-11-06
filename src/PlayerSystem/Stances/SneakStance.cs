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

    private bool _requiresSprintRelease;

    public string Name => "Sneak";
    public float SpeedMultiplier => Speed;
    public float NoiseRadius => Noise;

    public SneakStance(bool enteredWhileSprinting = false)
    {
        _requiresSprintRelease = enteredWhileSprinting;
    }

    public IPlayerStance HandleInput(PlayerInputCommand command)
    {
        // Reset latch once player lifts their finger off Shift
        if (!command.IsSprintHeld)
        {
            _requiresSprintRelease = false;
        }

        // Sprinting breaks out of sneak (only if Shift was freshly pressed)
        if (command.IsSprintHeld && !_requiresSprintRelease)
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
