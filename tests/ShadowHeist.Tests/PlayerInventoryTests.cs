using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem;
using ShadowHeist.PlayerSystem.Inventory;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class PlayerInventoryTests
{
    private const float FloatTolerance = 0.001f;

    [Fact]
    public void PlayerInventory_ComputesLinqTotalsAndCategoryCounts()
    {
        var inventory = new PlayerInventory();

        Assert.True(inventory.TryAddItem(new LootItem("loot_1", "Gold", 250_000, 10f)));
        Assert.True(inventory.TryAddItem(new LootItem("loot_2", "Cash", 150_000, 6f)));
        Assert.True(inventory.TryAddItem(new LootItem("loot_3", "Gold", 100_000, 4f)));

        Assert.Equal(3, inventory.Count);
        Assert.Equal(500_000, inventory.TotalCashValue);
        Assert.Equal(20f, inventory.TotalWeightKg, FloatTolerance);
        Assert.Equal(2, inventory.ItemCountsByCategory["Gold"]);
        Assert.Equal(1, inventory.ItemCountsByCategory["Cash"]);
        Assert.Equal(2, inventory.GetItemsByCategory("gold").Count);
        Assert.Equal("loot_1", inventory.GetHeaviestItems(1)[0].Id);
    }

    [Fact]
    public void PlayerInventory_RejectsDuplicateOrInvalidItems()
    {
        var inventory = new PlayerInventory();

        Assert.False(inventory.TryAddItem(new LootItem("", "Cash", 100, 1f)));
        Assert.False(inventory.TryAddItem(new LootItem("loot_neg", "Cash", -50, 1f)));
        Assert.False(inventory.TryAddItem(new LootItem("loot_w", "Cash", 50, -1f)));
        Assert.True(inventory.TryAddItem(new LootItem("loot_1", "Cash", 250_000, 10f)));
        Assert.False(inventory.TryAddItem(new LootItem("loot_1", "Cash", 250_000, 10f)));
        Assert.Equal(1, inventory.Count);
    }

    [Fact]
    public void PlayerInventory_MovementSpeedMultiplier_ScalesWithWeightAndClampsAtMinimum()
    {
        var inventory = new PlayerInventory();
        Assert.Equal(1f, inventory.MovementSpeedMultiplier, FloatTolerance);

        inventory.TryAddCashLoot("crate_1", 250_000);
        float expectedSingleCrateMultiplier = 1f - (10f * PlayerInventory.WeightSlowdownPerKg);
        Assert.Equal(expectedSingleCrateMultiplier, inventory.MovementSpeedMultiplier, FloatTolerance);

        for (int i = 2; i <= 10; i++)
        {
            inventory.TryAddCashLoot($"crate_{i}", 500_000);
        }

        Assert.Equal(PlayerInventory.MinSpeedMultiplier, inventory.MovementSpeedMultiplier, FloatTolerance);
    }

    [Fact]
    public void Player_MoveDistanceScalesWithCarrySpeedMultiplier()
    {
        var player = new Player(new Vector2(100f, 100f));
        var input = new PlayerInputCommand(
            MoveDirection: new Vector2(1f, 0f),
            AimWorldPosition: new Vector2(200f, 100f),
            IsSneakToggled: false,
            IsSprintHeld: false,
            IsInteractPressed: false,
            IsUseGadgetPressed: false);

        player.Update(input, 1f, Array.Empty<RectangleBounds>(), new GameEventBus(), carrySpeedMultiplier: 0.76f);

        Assert.Equal(100f + (190f * 0.76f), player.Position.X, 0.05f);
        Assert.Equal(0.76f, player.CarrySpeedMultiplier, FloatTolerance);
    }

    [Fact]
    public void PlayerSystemModule_SyncsLootFromAnyMapAndResetsOnInit()
    {
        using var context = new GameContext();
        var stubInput = new StubInputProvider(new PlayerInputCommand(
            MoveDirection: Vector2.Zero,
            AimWorldPosition: Vector2.Zero,
            IsSneakToggled: false,
            IsSprintHeld: false,
            IsInteractPressed: false,
            IsUseGadgetPressed: false));
        using var module = new PlayerSystemModule(stubInput);
        module.Init(context);

        context.StolenCash += 250_000;
        context.EventBus.Publish(new NoiseEmittedEvent(Vector2.Zero, 60f, "LootCrate:custom_map_crate_a"));
        module.Update(0.016f, context);

        Assert.Equal(1, module.Inventory.Count);
        Assert.Equal(250_000, module.Inventory.TotalCashValue);
        Assert.Equal("custom_map_crate_a", module.Inventory.Items[0].Id);
        Assert.True(module.Inventory.MovementSpeedMultiplier < 1f);

        context.StolenCash = 0;
        module.Init(context);
        Assert.Equal(0, module.Inventory.Count);
        Assert.Equal(1f, module.Inventory.MovementSpeedMultiplier, FloatTolerance);
    }

    private sealed class StubInputProvider : IInputProvider
    {
        private readonly PlayerInputCommand _command;
        public StubInputProvider(PlayerInputCommand command) => _command = command;
        public PlayerInputCommand ReadInput() => _command;
    }
}
