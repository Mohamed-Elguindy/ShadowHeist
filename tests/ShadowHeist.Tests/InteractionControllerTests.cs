using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld;
using ShadowHeist.HeistWorld.Interactables;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class InteractionControllerTests
{
    private static PlayerInputCommand CreateInput(bool isPressed = false, bool isHeld = false) =>
        new(
            MoveDirection: Vector2.Zero,
            AimWorldPosition: Vector2.Zero,
            IsSneakToggled: false,
            IsSprintHeld: false,
            IsInteractPressed: isPressed,
            IsUseGadgetPressed: false,
            IsInteractHeld: isHeld
        );

    [Fact]
    public void HandlePlayerInteraction_PressesNearbyLootCrate_WhenIsInteractPressed()
    {
        using var context = new GameContext();
        var crate = new LootCrate("crate_1", new Vector2(100f, 100f), cashValue: 10_000);
        context.Interactables.Add(crate);
        context.PlayerPosition = new Vector2(100f, 100f);
        context.CurrentInput = CreateInput(isPressed: true);

        var controller = new InteractionController();
        controller.HandlePlayerInteraction(deltaTime: 0.16f, context);

        Assert.True(crate.IsCollected);
        Assert.Equal(10_000, context.StolenCash);
    }

    [Fact]
    public void HandlePlayerInteraction_HoldsNearbyKeypad_WhenIsInteractHeld()
    {
        using var context = new GameContext();
        var keypad = new KeypadTerminal("kp_1", new Vector2(100f, 100f), targetId: "door_1");
        context.Interactables.Add(keypad);
        context.PlayerPosition = new Vector2(100f, 100f);
        context.CurrentInput = CreateInput(isHeld: true);

        var controller = new InteractionController();
        controller.HandlePlayerInteraction(deltaTime: 1.5f, context);

        Assert.True(keypad.HoldProgress > 0.4f);
        Assert.False(keypad.IsHacked);
    }

    [Fact]
    public void HandlePlayerInteraction_CancelsActiveHold_WhenInteractReleased()
    {
        using var context = new GameContext();
        var keypad = new KeypadTerminal("kp_1", new Vector2(100f, 100f), targetId: "door_1");
        context.Interactables.Add(keypad);
        context.PlayerPosition = new Vector2(100f, 100f);

        var controller = new InteractionController();

        // Frame 1: Hold is active
        context.CurrentInput = CreateInput(isHeld: true);
        controller.HandlePlayerInteraction(deltaTime: 1.0f, context);
        Assert.True(keypad.HoldProgress > 0f);

        // Frame 2: Released
        context.CurrentInput = CreateInput(isHeld: false);
        controller.HandlePlayerInteraction(deltaTime: 0.016f, context);

        Assert.Equal(0f, keypad.HoldProgress);
    }
}
