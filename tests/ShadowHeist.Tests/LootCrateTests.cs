using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.HeistWorld.Interactables;
using Xunit;

namespace ShadowHeist.Tests;

/// <summary>
/// Unit tests for LootCrate verifying interaction range, cash increment, and event publishing.
/// </summary>
public sealed class LootCrateTests
{
    [Fact]
    public void CanInteract_WhenPlayerIsWithinRadius_ReturnsTrue()
    {
        // Arrange
        var crate = new LootCrate("crate_1", new Vector2(100f, 100f), cashValue: 100_000, interactRadius: 50f);
        var playerNearby = new Vector2(120f, 100f); // 20 units away (within 50)

        // Act
        bool canInteract = crate.CanInteract(playerNearby);

        // Assert
        Assert.True(canInteract);
    }

    [Fact]
    public void CanInteract_WhenPlayerIsFar_ReturnsFalse()
    {
        // Arrange
        var crate = new LootCrate("crate_1", new Vector2(100f, 100f), cashValue: 100_000, interactRadius: 50f);
        var playerFar = new Vector2(200f, 100f); // 100 units away (exceeds 50)

        // Act
        bool canInteract = crate.CanInteract(playerFar);

        // Assert
        Assert.False(canInteract);
    }

    [Fact]
    public void OnInteract_AwardsCash_AndMarksAsCollected()
    {
        // Arrange
        using var context = new GameContext();
        var crate = new LootCrate("crate_1", new Vector2(100f, 100f), cashValue: 250_000);

        // Act
        crate.OnInteract(context, context.EventBus);

        // Assert
        Assert.True(crate.IsCollected);
        Assert.Equal(250_000, context.StolenCash);
        Assert.False(crate.CanInteract(new Vector2(100f, 100f))); // Cannot loot twice
    }

    [Fact]
    public void OnInteract_PublishesNoiseEmittedEvent()
    {
        // Arrange
        using var context = new GameContext();
        var crate = new LootCrate("crate_1", new Vector2(300f, 400f));
        NoiseEmittedEvent? heardEvent = null;
        context.EventBus.Subscribe<NoiseEmittedEvent>(evt => heardEvent = evt);

        // Act
        crate.OnInteract(context, context.EventBus);

        // Assert
        Assert.NotNull(heardEvent);
        Assert.Equal(new Vector2(300f, 400f), heardEvent.Value.Origin);
        Assert.Contains("LootCrate", heardEvent.Value.SourceName);
    }
}
