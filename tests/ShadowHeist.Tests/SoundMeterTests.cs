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
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);

        Assert.Equal(0f, meter.Level);
    }

    [Fact]
    public void NoiseEmitted_SetsLevelProportionally()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);

        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius / 2f, "Test"));

        Assert.Equal(0.5f, meter.Level, 0.001f);
    }

    [Fact]
    public void NoiseEmitted_ClampsAtOne()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);

        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 1.5f, "Test"));

        Assert.Equal(1f, meter.Level);
    }

    [Fact]
    public void NoiseEmitted_QuieterNoiseDoesNotLowerLevel()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);

        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 0.8f, "Loud"));
        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius * 0.3f, "Quiet"));

        Assert.Equal(0.8f, meter.Level, 0.001f);
    }

    [Fact]
    public void Update_DecaysLinearly()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);
        bus.Publish(new NoiseEmittedEvent(TestOrigin, SoundMeter.FullMeterNoiseRadius, "Test"));

        meter.Update(0.5f);

        float expected = 1f - (SoundMeter.DecayPerSecond * 0.5f);
        Assert.Equal(expected, meter.Level, 0.001f);
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
    public void NoiseEmitted_IgnoresZeroOrNegativeRadiusNoise()
    {
        var bus = new GameEventBus();
        var meter = new SoundMeter(bus);

        bus.Publish(new NoiseEmittedEvent(TestOrigin, 0f, "Silent"));
        bus.Publish(new NoiseEmittedEvent(TestOrigin, -10f, "Invalid"));

        Assert.Equal(0f, meter.Level);
    }
}
