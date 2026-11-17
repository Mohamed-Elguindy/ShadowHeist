using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class PlayerTests
{
    [Fact]
    public void Player_MovesPosition_WhenMoveDirectionProvided()
    {
        // Arrange
        var player = new Player(new Vector2(100f, 100f));
        var input = new PlayerInputCommand(
            MoveDirection: new Vector2(1f, 0f),
            AimWorldPosition: new Vector2(200f, 100f),
            IsSneakToggled: false,
            IsSprintHeld: false,
            IsInteractPressed: false,
            IsUseGadgetPressed: false
        );

        // Act (delta time = 1.0s, base speed = 190f)
        player.Update(input, 1f, Array.Empty<RectangleBounds>(), new GameEventBus());

        // Assert
        Assert.Equal(290f, player.Position.X, 0.01f);
        Assert.Equal(100f, player.Position.Y, 0.01f);
    }

    [Fact]
    public void Player_StaysStationary_WhenNoMoveInput()
    {
        // Arrange
        var initialPos = new Vector2(150f, 250f);
        var player = new Player(initialPos);
        var input = new PlayerInputCommand(
            MoveDirection: Vector2.Zero,
            AimWorldPosition: initialPos,
            IsSneakToggled: false,
            IsSprintHeld: false,
            IsInteractPressed: false,
            IsUseGadgetPressed: false
        );

        // Act
        player.Update(input, 0.5f, Array.Empty<RectangleBounds>(), new GameEventBus());

        // Assert
        Assert.Equal(initialPos, player.Position);
    }

    [Fact]
    public void Player_CannotTunnelThroughVaultWall_OnLargeStep()
    {
        var player = new Player(new Vector2(300f, 250f));
        var vaultWall = new[] { new RectangleBounds(320f, 160f, 32f, 256f) };
        var input = new PlayerInputCommand(
            MoveDirection: new Vector2(1f, 0f),
            AimWorldPosition: new Vector2(400f, 250f),
            IsSneakToggled: false,
            IsSprintHeld: true,
            IsInteractPressed: false,
            IsUseGadgetPressed: false);

        player.Update(input, 0.4f, vaultWall, new GameEventBus());

        Assert.True(player.Position.X <= 320f - player.Radius + 0.01f);
    }
}

