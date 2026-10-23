using System.Numerics;
using ShadowHeist.Core.Events;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Sample xUnit test demonstrating how decoupled C# logic is tested without opening a Raylib window.
/// </summary>
public sealed class CoreEventBusTests
{
    [Fact]
    public void Publish_NoiseEmittedEvent_NotifiesSubscribedListeners()
    {
        // Arrange
        var bus = new GameEventBus();
        NoiseEmittedEvent? receivedEvent = null;
        bus.Subscribe<NoiseEmittedEvent>(evt => receivedEvent = evt);

        var expectedOrigin = new Vector2(200f, 350f);

        // Act
        bus.Publish(new NoiseEmittedEvent(expectedOrigin, 180f, "CoinGadget"));

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(expectedOrigin, receivedEvent.Value.Origin);
        Assert.Equal(180f, receivedEvent.Value.Radius);
        Assert.Equal("CoinGadget", receivedEvent.Value.SourceName);
    }
}
