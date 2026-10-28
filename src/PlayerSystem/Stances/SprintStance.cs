using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem.Stances;

/// <summary>
/// Sprint stance (Ticket #11 - Mahmoud).
/// High speed boost but emits a loud noise radius.
/// </summary>
public sealed class SprintStance : IPlayerStance
{
    private const float Speed = 1.6f;
    private const float Noise = 220f;

    public string Name => "Sprint";
    public float SpeedMultiplier => Speed;
    public float NoiseRadius => Noise;

    public IPlayerStance HandleInput(PlayerInputCommand command)
    {
        // Releasing Shift returns to walk
        if (!command.IsSprintHeld)
        {
            return new WalkStance();
        }

        // Pressing Ctrl toggles into sneak
        if (command.IsSneakToggled)
        {
            return new SneakStance();
        }

        return this;
    }
}

