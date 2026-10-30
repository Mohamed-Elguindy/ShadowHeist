using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class NoiseEmissionTests
{
    private static PlayerInputCommand CreateInputCommand(Vector2 moveDir, bool isSneak = false, bool isSprint = false) =>
        new(
            MoveDirection: moveDir,
            AimWorldPosition: Vector2.Zero,
            IsSneakToggled: isSneak,
            IsSprintHeld: isSprint,
            IsInteractPressed: false,
            IsUseGadgetPressed: false
        );

    [Fact]
    public void Player_EmitsLoudNoise_WhenSprinting()
    {
        // Arrange
        var player = new Player(new Vector2(100f, 100f));
        var bus = new GameEventBus();
        NoiseEmittedEvent? capturedEvent = null;
        bus.Subscribe<NoiseEmittedEvent>(evt => capturedEvent = evt);

        var sprintInput = CreateInputCommand(moveDir: new Vector2(1f, 0f), isSprint: true);

        // Act
        player.Update(sprintInput, 0.5f, Array.Empty<RectangleBounds>(), bus);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(220f, capturedEvent.Value.Radius);
        Assert.Contains("Sprint", capturedEvent.Value.SourceName);
    }

    [Fact]
    public void Player_EmitsNoNoise_WhenSneaking()
    {
        // Arrange
        var player = new Player(new Vector2(100f, 100f));
        var bus = new GameEventBus();
        NoiseEmittedEvent? capturedEvent = null;
        bus.Subscribe<NoiseEmittedEvent>(evt => capturedEvent = evt);

        var sneakInput = CreateInputCommand(moveDir: new Vector2(1f, 0f), isSneak: true);

        // Act
        player.Update(sneakInput, 0.5f, Array.Empty<RectangleBounds>(), bus);

        // Assert
        Assert.Null(capturedEvent);
    }

    [Fact]
    public void Player_EmitsNoNoise_WhenStandingStill()
    {
        // Arrange
        var player = new Player(new Vector2(100f, 100f));
        var bus = new GameEventBus();
        NoiseEmittedEvent? capturedEvent = null;
        bus.Subscribe<NoiseEmittedEvent>(evt => capturedEvent = evt);

        var idleInput = CreateInputCommand(moveDir: Vector2.Zero);

        // Act
        player.Update(idleInput, 0.5f, Array.Empty<RectangleBounds>(), bus);

        // Assert
        Assert.Null(capturedEvent);
    }
}

