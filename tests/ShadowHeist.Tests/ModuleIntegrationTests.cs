using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.GuardAI;
using ShadowHeist.GuardAI.States;
using ShadowHeist.HeistWorld;
using ShadowHeist.PlayerSystem;
using ShadowHeist.WorldLighting;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class ModuleIntegrationTests
{
    private sealed class StubInputProvider : IInputProvider
    {
        public PlayerInputCommand NextCommand { get; set; }
        public PlayerInputCommand ReadInput() => NextCommand;
    }

    [Fact]
    public void FullModulePipeline_SprintLaserAndRestart_WorksEndToEndHeadless()
    {
        var input = new StubInputProvider();
        using var context = new GameContext();
        var world = new WorldLightingModule();
        var player = new PlayerSystemModule(input);
        var guards = new GuardAiModule(world);
        var heist = new HeistWorldModule(world);

        context.RegisterSystem(world);
        context.RegisterSystem(player);
        context.RegisterSystem(guards);
        context.RegisterSystem(heist);
        context.InitializeAll();

        // Sprinting raises SoundMeterLevel and keeps player aim/vision cone active
        input.NextCommand = new PlayerInputCommand(
            MoveDirection: Vector2.UnitX,
            AimWorldPosition: new Vector2(400f, 640f),
            IsSneakToggled: false,
            IsSprintHeld: true,
            IsInteractPressed: false,
            IsUseGadgetPressed: false);

        context.UpdateAll(0.25f);
        Assert.True(heist.SoundMeterLevel > 0f);
        Assert.NotEmpty(world.PlayerVisionCone);

        // Moving across the south hallway laser beam (x=336, y=417..736) triggers alarm & alerts guards
        context.PlayerPosition = new Vector2(336f, 520f);
        world.Update(0.016f, context);
        guards.Update(0.016f, context);

        Assert.True(context.IsAlarmTriggered);
        Assert.Contains(guards.Guards, g => g.CurrentState is SuspiciousState);

        // Restarting mission resets alarm, cash, sound meter, and laser states cleanly
        context.RestartMission();
        Assert.False(context.IsAlarmTriggered);
        Assert.Equal(0, context.StolenCash);
        Assert.Equal(0f, heist.SoundMeterLevel);
        Assert.All(world.Lasers, l => Assert.False(l.HasTriggeredAlarm));
    }
}
