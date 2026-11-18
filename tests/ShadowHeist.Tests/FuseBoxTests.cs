using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld;
using ShadowHeist.HeistWorld.Interactables;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class FuseBoxTests
{
    private const string FuseBoxId = "fusebox_lobby";
    private const string TargetGroupId = "lobby_lights";
    private static readonly Vector2 FuseBoxPosition = new(200f, 200f);

    [Fact]
    public void CanInteract_WhenPoweredAndPlayerWithinRadius_ReturnsTrue()
    {
        var fuseBox = new FuseBox(FuseBoxId, FuseBoxPosition, TargetGroupId, interactRadius: 55f);

        bool canInteract = fuseBox.CanInteract(new Vector2(240f, 200f));

        Assert.True(canInteract);
        Assert.Equal("[E] Cut Power", fuseBox.PromptText);
    }

    [Fact]
    public void CanInteract_WhenPlayerOutsideRadius_ReturnsFalse()
    {
        var fuseBox = new FuseBox(FuseBoxId, FuseBoxPosition, TargetGroupId, interactRadius: 55f);

        bool canInteract = fuseBox.CanInteract(new Vector2(260f, 200f));

        Assert.False(canInteract);
    }

    [Fact]
    public void OnInteract_WhenPowered_CutsPowerAndPublishesPowerStateChangedEvent()
    {
        using var context = new GameContext();
        var fuseBox = new FuseBox(FuseBoxId, FuseBoxPosition, TargetGroupId);
        PowerStateChangedEvent? published = null;
        context.EventBus.Subscribe<PowerStateChangedEvent>(evt => published = evt);

        fuseBox.OnInteract(context, context.EventBus);

        Assert.False(fuseBox.IsPowered);
        Assert.NotNull(published);
        Assert.Equal(TargetGroupId, published.Value.TargetGroupId);
        Assert.False(published.Value.IsPowered);
    }

    [Fact]
    public void OnInteract_CalledTwice_PublishesEventOnlyOnce()
    {
        using var context = new GameContext();
        var fuseBox = new FuseBox(FuseBoxId, FuseBoxPosition, TargetGroupId);
        int publishCount = 0;
        context.EventBus.Subscribe<PowerStateChangedEvent>(_ => publishCount++);

        fuseBox.OnInteract(context, context.EventBus);
        fuseBox.OnInteract(context, context.EventBus);

        Assert.Equal(1, publishCount);
        Assert.False(fuseBox.IsPowered);
    }

    [Fact]
    public void CanInteract_AfterPowerCut_ReturnsFalseAndClearsPromptText()
    {
        using var context = new GameContext();
        var fuseBox = new FuseBox(FuseBoxId, FuseBoxPosition, TargetGroupId);
        fuseBox.OnInteract(context, context.EventBus);

        Assert.False(fuseBox.CanInteract(FuseBoxPosition));
        Assert.Equal(string.Empty, fuseBox.PromptText);
    }

    [Fact]
    public void InteractionController_WhenInteractPressedInRange_CutsFuseBoxPower()
    {
        using var context = new GameContext();
        var fuseBox = new FuseBox(FuseBoxId, FuseBoxPosition, TargetGroupId);
        context.Interactables.Add(fuseBox);
        context.PlayerPosition = FuseBoxPosition;
        context.CurrentInput = CreateInteractInput(isPressed: true);

        PowerStateChangedEvent? published = null;
        context.EventBus.Subscribe<PowerStateChangedEvent>(evt => published = evt);

        var controller = new InteractionController();
        controller.HandlePlayerInteraction(0.016f, context);

        Assert.False(fuseBox.IsPowered);
        Assert.NotNull(published);
        Assert.Equal(TargetGroupId, published.Value.TargetGroupId);
        Assert.False(published.Value.IsPowered);
    }

    [Fact]
    public void InteractableQuery_WhenWallBlocksLineOfSight_ReturnsNull()
    {
        using var context = new GameContext();
        context.PlayerPosition = new Vector2(100f, 200f);
        context.Walls.Add(new RectangleBounds(140f, 150f, 20f, 100f));
        context.Interactables.Add(new FuseBox(FuseBoxId, FuseBoxPosition, TargetGroupId, interactRadius: 150f));

        IInteractable? result = InteractableQuery.FindInPlayerRange(context);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("", TargetGroupId)]
    [InlineData("   ", TargetGroupId)]
    [InlineData(FuseBoxId, "")]
    [InlineData(FuseBoxId, "   ")]
    public void Constructor_WithBlankIdentifiers_ThrowsArgumentException(string id, string targetGroupId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FuseBox(id, FuseBoxPosition, targetGroupId));
    }

    private static PlayerInputCommand CreateInteractInput(bool isPressed) =>
        new(
            MoveDirection: Vector2.Zero,
            AimWorldPosition: Vector2.Zero,
            IsSneakToggled: false,
            IsSprintHeld: false,
            IsInteractPressed: isPressed,
            IsUseGadgetPressed: false,
            IsInteractHeld: false);
}
