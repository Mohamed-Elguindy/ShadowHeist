using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.WorldLighting;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class LaserSwitchTests
{
    private const string SwitchId = "laser_switch_hall";
    private const string TargetGroupId = "laser_hall";
    private static readonly Vector2 SwitchPosition = new(284f, 444f);

    [Fact]
    public void CanInteract_WhenPlayerWithinRadius_ReturnsTrueInBothStates()
    {
        using var context = new GameContext();
        var laserSwitch = new LaserSwitch(SwitchId, SwitchPosition, TargetGroupId, interactRadius: 55f);

        Assert.True(laserSwitch.CanInteract(new Vector2(310f, 444f)));

        laserSwitch.OnInteract(context, context.EventBus);

        Assert.True(laserSwitch.CanInteract(new Vector2(310f, 444f)));
    }

    [Fact]
    public void CanInteract_WhenPlayerOutsideRadius_ReturnsFalse()
    {
        var laserSwitch = new LaserSwitch(SwitchId, SwitchPosition, TargetGroupId, interactRadius: 55f);

        Assert.False(laserSwitch.CanInteract(new Vector2(360f, 444f)));
    }

    [Fact]
    public void OnInteract_FirstPress_DisablesLasersAndUpdatesPrompt()
    {
        using var context = new GameContext();
        var laserSwitch = new LaserSwitch(SwitchId, SwitchPosition, TargetGroupId);
        PowerStateChangedEvent? published = null;
        context.EventBus.Subscribe<PowerStateChangedEvent>(evt => published = evt);

        laserSwitch.OnInteract(context, context.EventBus);

        Assert.False(laserSwitch.IsPowered);
        Assert.Equal("[E] Enable Lasers", laserSwitch.PromptText);
        Assert.NotNull(published);
        Assert.Equal(TargetGroupId, published.Value.TargetGroupId);
        Assert.False(published.Value.IsPowered);
    }

    [Fact]
    public void OnInteract_SecondPress_ReEnablesLasersAndRestoresPrompt()
    {
        using var context = new GameContext();
        var laserSwitch = new LaserSwitch(SwitchId, SwitchPosition, TargetGroupId);
        var events = new List<PowerStateChangedEvent>();
        context.EventBus.Subscribe<PowerStateChangedEvent>(events.Add);

        laserSwitch.OnInteract(context, context.EventBus);
        laserSwitch.OnInteract(context, context.EventBus);

        Assert.True(laserSwitch.IsPowered);
        Assert.Equal("[E] Disable Lasers", laserSwitch.PromptText);
        Assert.Equal(2, events.Count);
        Assert.False(events[0].IsPowered);
        Assert.True(events[1].IsPowered);
    }

    [Fact]
    public void InteractionController_PressingTwiceAcrossFrames_TogglesLaserSwitchOffThenOn()
    {
        using var context = new GameContext();
        var laserSwitch = new LaserSwitch(SwitchId, SwitchPosition, TargetGroupId);
        context.Interactables.Add(laserSwitch);
        context.PlayerPosition = SwitchPosition;
        context.CurrentInput = CreateInteractInput(isPressed: true);

        var controller = new InteractionController();
        controller.HandlePlayerInteraction(0.016f, context);
        Assert.False(laserSwitch.IsPowered);

        controller.HandlePlayerInteraction(0.016f, context);
        Assert.True(laserSwitch.IsPowered);
    }

    [Fact]
    public void HeistWorldModule_InitAndRestart_SpawnsAccessiblePowerDevicesAndResetsThem()
    {
        using var context = new GameContext();
        var world = new WorldLightingModule();
        var heist = new HeistWorldModule(world);
        context.RegisterSystem(world);
        context.RegisterSystem(heist);
        context.InitializeAll();

        FuseBox fuseBox = Assert.Single(context.Interactables.OfType<FuseBox>());
        LaserSwitch laserSwitch = Assert.Single(context.Interactables.OfType<LaserSwitch>());

        context.PlayerPosition = fuseBox.Position;
        Assert.Same(fuseBox, InteractableQuery.FindInPlayerRange(context));
        fuseBox.OnInteract(context, context.EventBus);

        context.PlayerPosition = laserSwitch.Position;
        Assert.Same(laserSwitch, InteractableQuery.FindInPlayerRange(context));
        laserSwitch.OnInteract(context, context.EventBus);

        Assert.False(fuseBox.IsPowered);
        Assert.False(laserSwitch.IsPowered);

        context.RestartMission();

        FuseBox resetFuseBox = Assert.Single(context.Interactables.OfType<FuseBox>());
        LaserSwitch resetLaserSwitch = Assert.Single(context.Interactables.OfType<LaserSwitch>());
        Assert.True(resetFuseBox.IsPowered);
        Assert.True(resetLaserSwitch.IsPowered);
    }

    [Theory]
    [InlineData("", TargetGroupId)]
    [InlineData("   ", TargetGroupId)]
    [InlineData(SwitchId, "")]
    [InlineData(SwitchId, "   ")]
    public void Constructor_WithBlankIdentifiers_ThrowsArgumentException(string id, string targetGroupId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new LaserSwitch(id, SwitchPosition, targetGroupId));
    }

    [Fact]
    public void LaserSwitch_WhenToggledThroughModules_DisablesAndReEnablesHallLaser()
    {
        using var context = new GameContext();
        var world = new WorldLightingModule();
        var heist = new HeistWorldModule(world);
        context.RegisterSystem(world);
        context.RegisterSystem(heist);
        context.InitializeAll();

        LaserTripwire hallLaser = Assert.Single(world.Lasers, l => l.Id == TargetGroupId);
        LaserSwitch laserSwitch = Assert.Single(context.Interactables.OfType<LaserSwitch>());
        Assert.True(hallLaser.IsActive);

        context.PlayerPosition = laserSwitch.Position;
        context.CurrentInput = CreateInteractInput(isPressed: true);
        context.UpdateAll(0.016f);

        Assert.False(hallLaser.IsActive);

        context.CurrentInput = CreateInteractInput(isPressed: false);
        context.PlayerPosition = new Vector2(336f, 520f);
        world.Update(0.016f, context);
        Assert.False(context.IsAlarmTriggered);

        context.PlayerPosition = laserSwitch.Position;
        context.CurrentInput = CreateInteractInput(isPressed: true);
        context.UpdateAll(0.016f);

        Assert.True(hallLaser.IsActive);

        context.CurrentInput = CreateInteractInput(isPressed: false);
        context.PlayerPosition = new Vector2(336f, 520f);
        world.Update(0.016f, context);
        Assert.True(context.IsAlarmTriggered);
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
