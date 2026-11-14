using System.Numerics;
using ShadowHeist.Core.Events;
using ShadowHeist.HeistWorld.UI;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class SoundMeterTests
{
    private static readonly Vector2 TestOrigin = new(100f, 100f);

    [Fact]
    public void InitialState_LevelIsZero()
    {
        var meter = new SoundMeter(new GameEventBus());

        Assert.Equal(0f, meter.Level);
        Assert.Equal(0f, meter.TargetLevel);
    }

    [Fact]
    public void NoiseEmitted_SetsTargetLevelProportionally()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);

        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius / 2f, "Test"));

        Assert.Equal(0.5f, meter.TargetLevel, 0.001f);
    }

    [Fact]
    public void NoiseEmitted_ClampsTargetAtOne()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);

        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 1.5f, "Test"));

        Assert.Equal(1f, meter.TargetLevel);
    }

    [Fact]
    public void Update_RampsLevelTowardsTargetLinearly()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);
        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius, "Test"));

        const float attackTime = 0.1f;
        meter.Update(attackTime);

        Assert.Equal(SoundMeter.AttackPerSecond * attackTime, meter.Level, 0.001f);
    }

    [Fact]
    public void Update_ReachesTargetLevel_WhenWithinHoldDuration()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);
        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 0.5f, "Test"));

        meter.Update(0.30f);

        Assert.Equal(0.5f, meter.Level, 0.001f);
    }

    [Fact]
    public void Update_DecaysLinearly_AfterHoldDurationExpires()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);
        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 0.5f, "Test"));
        meter.Update(0.30f);
        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 0.5f, "Test"));

        const float decayDelta = 0.2f;
        meter.Update(SoundMeter.PeakHoldDuration + decayDelta);

        Assert.Equal(0.5f - (SoundMeter.DecayPerSecond * decayDelta), meter.Level, 0.001f);
    }

    [Fact]
    public void NoiseEmitted_QuieterNoiseDuringHold_DoesNotLowerTargetOrExtendHold()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);
        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 0.8f, "Loud"));
        meter.Update(0.20f);

        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 0.3f, "Quiet"));
        Assert.Equal(0.8f, meter.TargetLevel, 0.001f);

        meter.Update(0.20f);
        Assert.Equal(0f, meter.TargetLevel);
    }

    [Fact]
    public void Update_ClampsAtZero()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);
        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 0.5f, "Test"));

        meter.Update(10f);

        Assert.Equal(0f, meter.Level);
    }

    [Fact]
    public void ContinuousSteps_BuildsLevelGraduallyOverMultipleSteps()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);
        const float sprintRadius = 220f;
        float target = sprintRadius / SoundMeter.FullMeterNoiseRadius;

        bus.Publish(new NoiseEmittedEvent(TestOrigin, sprintRadius, "SprintStep1"));
        meter.Update(0.25f);
        float step1Level = meter.Level;

        bus.Publish(new NoiseEmittedEvent(TestOrigin, sprintRadius, "SprintStep2"));
        meter.Update(0.25f);

        Assert.True(step1Level < target);
        Assert.Equal(target, meter.Level, 0.001f);
    }

    [Theory]
    [InlineData(0f, NoiseCategory.Player)]
    [InlineData(-10f, NoiseCategory.Player)]
    [InlineData(200f, NoiseCategory.Distraction)]
    [InlineData(200f, NoiseCategory.Environment)]
    public void NoiseEmitted_IgnoresIrrelevantNoise(float radius, NoiseCategory category)
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);

        bus.Publish(new NoiseEmittedEvent(TestOrigin, radius, "Ignored", category));

        Assert.Equal(0f, meter.TargetLevel);
        Assert.Equal(0f, meter.Level);
    }
}
