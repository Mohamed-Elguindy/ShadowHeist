using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.PlayerSystem.Input;

// <summary>
/// Hardware keyboard and mouse implementation of IInputProvider (Ticket #3 - Mahmoud).


public sealed class KeyboardInputProvider : IInputProvider
{
    public PlayerInputCommand ReadInput()
    {
        // Vector2 stores 2D values as (X, Y).
        // moveDirection: represents the direction the player wants to move (-1 to 1 on X and Y).
        Vector2 moveDirection = ReadMoveDirection();

        // mousePosition: represents the cursor's current (X, Y) pixel position on the screen.
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
        // Vector2.Zero represents (X: 0, Y: 0), meaning the player is not pressing any movement keys.
        var direction = Vector2.Zero;

        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) direction.Y -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) direction.Y += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) direction.X -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) direction.X += 1f;

        // If the player is pressing at least one movement key, direction is not (0, 0)
        if (direction != Vector2.Zero)
        {
            // Vector2.Normalize rescales the vector so its total length equals 1.
            // Why we need this:  Without Normalize(), the player would move ~41% faster diagonally
            return Vector2.Normalize(direction);
        }

        // No keys pressed, return (0, 0)
        return Vector2.Zero;
    }
}
