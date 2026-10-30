using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem.Input;

/// <summary>
/// Hardware keyboard and mouse implementation of IInputProvider (Ticket #3 - Mahmoud).
/// </summary>
public sealed class KeyboardInputProvider : IInputProvider
{
    public PlayerInputCommand ReadInput()
    {
        Vector2 moveDirection = ReadMoveDirection();
        Vector2 mousePosition = Raylib.GetMousePosition();

        bool isSneakToggled = Raylib.IsKeyPressed(KeyboardKey.LeftControl);
        bool isSprintHeld = Raylib.IsKeyDown(KeyboardKey.LeftShift);
        bool isInteractPressed = Raylib.IsKeyPressed(KeyboardKey.E);
        bool isInteractHeld = Raylib.IsKeyDown(KeyboardKey.E);
        bool isUseGadgetPressed = Raylib.IsKeyPressed(KeyboardKey.Q) || Raylib.IsKeyPressed(KeyboardKey.Space);

        return new PlayerInputCommand(
            moveDirection,
            mousePosition,
            isSneakToggled,
            isSprintHeld,
            isInteractPressed,
            isUseGadgetPressed,
            isInteractHeld
        );
    }

    private static Vector2 ReadMoveDirection()
    {
        var direction = Vector2.Zero;

        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) direction.Y -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) direction.Y += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) direction.X -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) direction.X += 1f;

        if (direction != Vector2.Zero)
        {
            return Vector2.Normalize(direction);
        }

        return Vector2.Zero;
    }
}
