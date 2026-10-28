using System.Numerics;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Stances;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class StanceTests
{
    private static PlayerInputCommand CreateCommand(bool isSneakToggled = false, bool isSprintHeld = false) =>
        new(
            MoveDirection: Vector2.Zero,
            AimWorldPosition: Vector2.Zero,
            IsSneakToggled: isSneakToggled,
            IsSprintHeld: isSprintHeld,
            IsInteractPressed: false,
            IsUseGadgetPressed: false
        );

    [Fact]
    public void WalkStance_TransitionsToSneak_WhenSneakToggled()
    {
        var stance = new WalkStance();
        var nextStance = stance.HandleInput(CreateCommand(isSneakToggled: true));

        Assert.IsType<SneakStance>(nextStance);
        Assert.Equal(0.55f, nextStance.SpeedMultiplier);
        Assert.Equal(0f, nextStance.NoiseRadius);
    }

    [Fact]
    public void WalkStance_TransitionsToSprint_WhenSprintHeld()
    {
        var stance = new WalkStance();
        var nextStance = stance.HandleInput(CreateCommand(isSprintHeld: true));

        Assert.IsType<SprintStance>(nextStance);
        Assert.Equal(1.6f, nextStance.SpeedMultiplier);
        Assert.Equal(220f, nextStance.NoiseRadius);
    }

    [Fact]
    public void SprintStance_TransitionsBackToWalk_WhenSprintReleased()
    {
        var stance = new SprintStance();
        var nextStance = stance.HandleInput(CreateCommand(isSprintHeld: false));

        Assert.IsType<WalkStance>(nextStance);
    }

    [Fact]
    public void SneakStance_TransitionsBackToWalk_WhenSneakToggledAgain()
    {
        var stance = new SneakStance();
        var nextStance = stance.HandleInput(CreateCommand(isSneakToggled: true));

        Assert.IsType<WalkStance>(nextStance);
    }
}

