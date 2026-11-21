namespace ShadowHeist.PlayerSystem.Inventory;

/// <summary>
/// Immutable loot item entry tracked by PlayerInventory for LINQ weight and cash totals.
/// </summary>
public readonly record struct LootItem(
    string Id,
    string Category,
    int CashValue,
    float WeightKg);
