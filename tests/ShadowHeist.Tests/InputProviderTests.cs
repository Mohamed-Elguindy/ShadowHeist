using System.Numerics;
using ShadowHeist.Core.Interfaces;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Unit tests demonstrating the power of IInputProvider abstraction (Ticket #3 - Mahmoud).
/// Allows testing movement commands without requiring physical hardware or a Raylib window.
/// </summary>
public sealed class InputProviderTests
{
    private sealed class MockInputProvider : IInputProvider
    {
        public PlayerInputCommand CommandToReturn { get; set; }

        public PlayerInputCommand ReadInput() => CommandToReturn;
    }

    [Fact]
    public void MockInputProvider_ReturnsConfiguredPlayerInputCommand()
    {
        // Arrange
        var mock = new MockInputProvider
        {
            CommandToReturn = new PlayerInputCommand(
                MoveDirection: Vector2.Normalize(new Vector2(1f, 1f)),
                AimWorldPosition: new Vector2(400f, 300f),
                IsSneakToggled: false,
                IsSprintHeld: true,
                IsInteractPressed: true,
                IsUseGadgetPressed: false,
                IsInteractHeld: true
            )
        };

        // Act
        PlayerInputCommand command = mock.ReadInput();

        // Assert
        Assert.True(command.IsSprintHeld);
        Assert.True(command.IsInteractPressed);
        Assert.True(command.IsInteractHeld);
        Assert.False(command.IsSneakToggled);
        Assert.False(command.IsUseGadgetPressed);
        Assert.Equal(new Vector2(400f, 300f), command.AimWorldPosition);
        Assert.True(command.MoveDirection.LengthSquared() > 0.99f && command.MoveDirection.LengthSquared() < 1.01f);
    }

    [Fact]
    public void PlayerSystemModule_AcceptsInjectedInputProvider_ForDependencyInjection()
    {
        var mock = new MockInputProvider();
        using var module = new ShadowHeist.PlayerSystem.PlayerSystemModule(mock);

        Assert.NotNull(module);
    }
}

