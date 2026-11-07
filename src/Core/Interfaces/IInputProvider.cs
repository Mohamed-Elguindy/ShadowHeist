using System.Numerics;

namespace ShadowHeist.Core.Interfaces;

/// <summary>
/// Immutable snapshot of player inputs for a single frame.
/// Allows Player movement and stances to be tested without a live keyboard/mouse.
/// </summary>
public readonly record struct PlayerInputCommand(
    Vector2 MoveDirection,
    Vector2 AimWorldPosition,
    bool IsSneakToggled,
    bool IsSprintHeld,
    bool IsInteractPressed,
    bool IsUseGadgetPressed,
    bool IsInteractHeld = false
);

/// <summary>
/// Abstraction over hardware input (Ticket #3 — Mahmoud).
/// </summary>
public interface IInputProvider
{
    PlayerInputCommand ReadInput();
}
