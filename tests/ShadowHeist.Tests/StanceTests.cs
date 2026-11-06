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

    [Fact]
    public void SneakStance_TransitionsToSprint_WhenSprintHeld()
    {
        var stance = new SneakStance();
        var nextStance = stance.HandleInput(CreateCommand(isSprintHeld: true));

        Assert.IsType<SprintStance>(nextStance);
    }

    [Fact]
    public void SprintStance_EmergencyBrakesIntoSneak_WhenSneakToggledWhileSprinting()
    {
        var stance = new SprintStance();
        var nextStance = stance.HandleInput(CreateCommand(isSneakToggled: true, isSprintHeld: true));

        Assert.IsType<SneakStance>(nextStance);
    }

    [Fact]
    public void SneakStance_MaintainsSneak_WhileShiftRemainsHeldAfterSprintBrake()
    {
        var stance = new SneakStance(enteredWhileSprinting: true);

        // Next frame: Shift is still down from sprint, but player hasn't pressed Ctrl again
        var nextStance = stance.HandleInput(CreateCommand(isSneakToggled: false, isSprintHeld: true));

        // Must stay in sneak, not snap back to sprint!
        Assert.IsType<SneakStance>(nextStance);
    }

    [Fact]
    public void SneakStance_AllowsSprintAgain_OnlyAfterShiftReleasedAndRePressed()
    {
        var stance = new SneakStance(enteredWhileSprinting: true);

        // Step 1: Player releases Shift
        var releasedStance = stance.HandleInput(CreateCommand(isSprintHeld: false));
        Assert.IsType<SneakStance>(releasedStance);

        // Step 2: Player presses Shift again -> now can sprint
        var sprintStance = releasedStance.HandleInput(CreateCommand(isSprintHeld: true));
        Assert.IsType<SprintStance>(sprintStance);
    }
}

