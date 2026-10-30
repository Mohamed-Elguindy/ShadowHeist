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
        // Emergency brake: pressing Ctrl while sprinting immediately drops into sneak
        if (command.IsSneakToggled)
        {
            return new SneakStance(enteredWhileSprinting: true);
        }

        // Releasing Shift returns to walk
        if (!command.IsSprintHeld)
        {
            return new WalkStance();
        }

        return this;
    }
}

