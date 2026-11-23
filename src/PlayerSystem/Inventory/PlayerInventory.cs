namespace ShadowHeist.PlayerSystem.Inventory;

/// <summary>
/// Tracks collected loot items using collections and LINQ to compute total cash, weight, and carry slowdown.
/// </summary>
public sealed class PlayerInventory
{
    public const float CashPerKilogram = 25_000f;
    public const float MinItemWeightKg = 1f;
    public const float MaxItemWeightKg = 15f;
    public const float WeightSlowdownPerKg = 0.012f;
    public const float MinSpeedMultiplier = 0.65f;
    public const float FullSpeedMultiplier = 1f;

    private const string DefaultLootCategory = "Cash";

    private readonly List<LootItem> _items = new();
    private readonly Dictionary<string, int> _itemCountsByCategory = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _collectedIds = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<LootItem> Items => _items;
    public IReadOnlyDictionary<string, int> ItemCountsByCategory => _itemCountsByCategory;
    public int Count => _items.Count;
    public float TotalWeightKg => _items.Sum(item => item.WeightKg);
    public int TotalCashValue => _items.Sum(item => item.CashValue);

    public float MovementSpeedMultiplier => Math.Clamp(
        FullSpeedMultiplier - (TotalWeightKg * WeightSlowdownPerKg),
        MinSpeedMultiplier,
        FullSpeedMultiplier);

    public static float ComputeWeightForCash(int cashValue) =>
        cashValue <= 0
            ? 0f
            : Math.Clamp(cashValue / CashPerKilogram, MinItemWeightKg, MaxItemWeightKg);

    public bool TryAddCashLoot(string id, int cashValue, string category = DefaultLootCategory)
    {
        if (cashValue <= 0)
        {
            return false;
        }

        string resolvedCategory = string.IsNullOrWhiteSpace(category) ? DefaultLootCategory : category;
        float weightKg = ComputeWeightForCash(cashValue);
        return TryAddItem(new LootItem(id, resolvedCategory, cashValue, weightKg));
    }

    public bool TryAddItem(LootItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Id) ||
            string.IsNullOrWhiteSpace(item.Category) ||
            item.CashValue < 0 ||
            item.WeightKg < 0f)
        {
            return false;
        }

        if (!_collectedIds.Add(item.Id))
        {
            return false;
        }

        _items.Add(item);
        _itemCountsByCategory[item.Category] = _itemCountsByCategory.GetValueOrDefault(item.Category) + 1;
        return true;
    }

    public IReadOnlyList<LootItem> GetItemsByCategory(string category) =>
        string.IsNullOrWhiteSpace(category)
            ? Array.Empty<LootItem>()
            : _items.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<LootItem> GetHeaviestItems(int maxCount) =>
        maxCount <= 0
            ? Array.Empty<LootItem>()
            : _items.OrderByDescending(item => item.WeightKg).Take(maxCount).ToList();

    public void Clear()
    {
        _items.Clear();
        _itemCountsByCategory.Clear();
        _collectedIds.Clear();
    }
}
