using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.UI;
using ShadowHeist.PlayerSystem;
using ShadowHeist.PlayerSystem.Gadgets;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class GadgetTests
{
    private const float FloatTolerance = 0.01f;

    [Fact]
    public void CoinGadget_TryUse_PublishesDistractionNoiseWithoutSpikingSoundMeter()
    {
        var eventBus = new GameEventBus();
        var soundMeter = new SoundMeter(eventBus);
        NoiseEmittedEvent? capturedNoise = null;
        eventBus.Subscribe<NoiseEmittedEvent>(evt => capturedNoise = evt);

        var coin = new CoinGadget(initialCharges: 1, maxThrowDistance: 200f, noiseRadius: 280f);

        Assert.True(coin.TryUse(new Vector2(100f, 100f), new Vector2(220f, 100f), eventBus));
        soundMeter.Update(0.1f);

        Assert.Equal(0, coin.RemainingCharges);
        Assert.NotNull(capturedNoise);
        Assert.Equal(NoiseCategory.Distraction, capturedNoise!.Value.Category);
        Assert.Equal(280f, capturedNoise.Value.Radius, FloatTolerance);
        Assert.Equal(0f, soundMeter.Level, FloatTolerance);
        Assert.False(coin.TryUse(new Vector2(100f, 100f), new Vector2(220f, 100f), eventBus));
    }

    [Fact]
    public void CoinGadget_TryUse_StopsInFrontOfBlockingWallOnAnyMap()
    {
        var eventBus = new GameEventBus();
        var walls = new[] { new RectangleBounds(200f, 0f, 32f, 300f) };
        var coin = new CoinGadget(initialCharges: 2, maxThrowDistance: 300f);
        coin.SetWalls(walls);

        Assert.True(coin.TryUse(new Vector2(100f, 150f), new Vector2(350f, 150f), eventBus));

        Assert.True(coin.LastLandingPoint.X < 200f);
    }

    [Fact]
    public void EmpGadget_TryUse_CutsPowerAndRestoresWhenTimerExpires()
    {
        using var context = new GameContext();
        context.InitializeAll();
        var emp = new EmpGadget(initialCharges: 2, disableDurationSeconds: 6f);
        context.EventBus.Subscribe<PowerStateChangedEvent>(emp.ObserveExternalPowerEvent);

        Assert.True(emp.TryUse(Vector2.Zero, new Vector2(150f, 0f), context.EventBus));
        Assert.False(emp.TryUse(Vector2.Zero, Vector2.Zero, context.EventBus));
        Assert.Equal(1, emp.RemainingCharges);
        Assert.Equal(150f, emp.LastLandingPoint.X, FloatTolerance);
        Assert.False(context.AreLobbyLightsPowered);
        Assert.True(emp.IsActivePulse);

        emp.Update(3.0f, context.EventBus);
        Assert.True(emp.IsActivePulse);
        Assert.False(context.AreLobbyLightsPowered);

        emp.Update(3.5f, context.EventBus);
        Assert.False(emp.IsActivePulse);
        Assert.True(context.AreLobbyLightsPowered);
    }

    [Fact]
    public void EmpGadget_TryUse_ThrowsTowardAimAndStopsAtBlockingWall()
    {
        var eventBus = new GameEventBus();
        var walls = new[] { new RectangleBounds(210f, 0f, 32f, 300f) };
        var emp = new EmpGadget(initialCharges: 1, maxThrowDistance: 240f);
        emp.SetWalls(walls);

        Assert.True(emp.TryUse(new Vector2(100f, 150f), new Vector2(360f, 150f), eventBus));

        Assert.Equal(new Vector2(100f, 150f), emp.LastThrowOrigin);
        Assert.True(emp.LastLandingPoint.X > 100f && emp.LastLandingPoint.X < 210f);
    }

    [Fact]
    public void EmpGadget_PreservesPriorFuseBoxAndLaserSwitchOffStatesAfterExpiry()
    {
        var eventBus = new GameEventBus();
        var emp = new EmpGadget(initialCharges: 1, disableDurationSeconds: 4f);
        var powerLog = new List<PowerStateChangedEvent>();
        eventBus.Subscribe<PowerStateChangedEvent>(evt =>
        {
            emp.ObserveExternalPowerEvent(evt);
            powerLog.Add(evt);
        });

        eventBus.Publish(new PowerStateChangedEvent("lobby_lights", IsPowered: false));
        eventBus.Publish(new PowerStateChangedEvent("laser_hall", IsPowered: false));

        Assert.True(emp.TryUse(Vector2.Zero, Vector2.Zero, eventBus));
        emp.Update(4.5f, eventBus);

        Assert.False(powerLog.Last(e => e.TargetGroupId == "lobby_lights").IsPowered);
        Assert.False(powerLog.Last(e => e.TargetGroupId == "laser_hall").IsPowered);
    }

    [Fact]
    public void PlayerSystemModule_UseGadgetInput_TriggersActiveCoinAndEmpHeadless()
    {
        using var context = new GameContext { PlayerPosition = new Vector2(100f, 100f) };
        var stubInput = new MutableStubInputProvider();
        using var module = new PlayerSystemModule(stubInput);
        context.RegisterSystem(module);
        context.InitializeAll();

        stubInput.NextCommand = new PlayerInputCommand(
            MoveDirection: Vector2.Zero,
            AimWorldPosition: new Vector2(200f, 100f),
            IsSneakToggled: false,
            IsSprintHeld: false,
            IsInteractPressed: false,
            IsUseGadgetPressed: true);

        module.Update(0.016f, context);
        Assert.Equal(CoinGadget.DefaultCharges - 1, module.Gadgets.Coin.RemainingCharges);

        module.Gadgets.CycleNext();
        module.Update(0.016f, context);
        Assert.Equal(EmpGadget.DefaultCharges - 1, module.Gadgets.Emp.RemainingCharges);
        Assert.False(context.AreLobbyLightsPowered);
    }

    private sealed class MutableStubInputProvider : IInputProvider
    {
        public PlayerInputCommand NextCommand { get; set; }
        public PlayerInputCommand ReadInput() => NextCommand;
    }
}
