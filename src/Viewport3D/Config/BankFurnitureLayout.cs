using System.Numerics;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Declarative 3D furniture placement for Level 1 City Bank.
/// Kenney furniture assets face local -Z at 0 degrees.
/// </summary>
public static class BankFurnitureLayout
{
    private const float FacingNorth = 0f;
    private const float FacingSouth = 180f;
    private const float FacingWest = 90f;
    private const float FacingEast = -90f;

    public static IReadOnlyList<FurnitureSpec> LobbyFurniture { get; } = BuildLobbyFurniture();

    private static IReadOnlyList<FurnitureSpec> BuildLobbyFurniture()
    {
        var furniture = new List<FurnitureSpec>();
        furniture.AddRange(WestWaitingLounge());
        furniture.AddRange(EntrancePlants());
        furniture.AddRange(NorthSecurityHall());
        furniture.AddRange(TellerStation());
        furniture.AddRange(EastWaitingLounge());
        furniture.AddRange(EastCorridor());
        return furniture;
    }

    private static IEnumerable<FurnitureSpec> WestWaitingLounge() =>
    [
        new("rugRectangle",   new Vector2(156f, 440f), FacingWest,  165f),
        new("loungeSofa",     new Vector2(86f, 440f),  FacingEast,  105f),
        new("tableCoffee",    new Vector2(156f, 440f), FacingWest,   72f),
        new("loungeChair",    new Vector2(156f, 336f), FacingSouth,  52f),
        new("loungeChair",    new Vector2(156f, 544f), FacingNorth,  52f),
        new("lampRoundFloor", new Vector2(84f, 344f),  FacingNorth,  82f, EmitsWarmGlow: true)
    ];

    private static IEnumerable<FurnitureSpec> EntrancePlants() =>
    [
        new("pottedPlant", new Vector2(80f, 232f),   20f,        60f),
        new("pottedPlant", new Vector2(272f, 220f), -15f,        58f),
        new("plantSmall1", new Vector2(72f, 72f),   FacingNorth, 34f)
    ];

    private static IEnumerable<FurnitureSpec> NorthSecurityHall() =>
    [
        new("sideTableDrawers", new Vector2(640f, 52f),  FacingSouth, 60f),
        new("plantSmall1",      new Vector2(1208f, 72f), 45f,         34f)
    ];

    private static IEnumerable<FurnitureSpec> TellerStation() =>
    [
        new("loungeChair", new Vector2(560f, 604f), FacingNorth, 52f)
    ];

    private static IEnumerable<FurnitureSpec> EastWaitingLounge() =>
    [
        new("rugRectangle",   new Vector2(830f, 566f), FacingNorth, 165f),
        new("loungeSofa",     new Vector2(830f, 646f), FacingNorth, 105f),
        new("tableCoffee",    new Vector2(830f, 566f), FacingNorth,  72f),
        new("lampRoundFloor", new Vector2(912f, 646f), FacingNorth,  82f, EmitsWarmGlow: true)
    ];

    private static IEnumerable<FurnitureSpec> EastCorridor() =>
    [
        new("pottedPlant",      new Vector2(1200f, 688f), 30f,        60f),
        new("sideTableDrawers", new Vector2(1224f, 380f), FacingWest, 60f)
    ];
}
