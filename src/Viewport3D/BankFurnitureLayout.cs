using System.Numerics;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Declarative 3D furniture placement for Level 1 City Bank.
/// Note: Kenney furniture models face local -Z at 0 deg (+90 = West, -90 = East, 180 = South).
/// </summary>
public static class BankFurnitureLayout
{
    public static readonly IReadOnlyList<FurnitureSpec> LobbyFurniture = new FurnitureSpec[]
    {
        // West Waiting Lounge (sofa backs West wall -> faces East -90; chairs face inward toward coffee table)
        new("rugRectangle",   new Vector2(156f, 440f),  90f, 165f),
        new("loungeSofa",     new Vector2(86f, 440f),  -90f, 105f),
        new("tableCoffee",    new Vector2(156f, 440f),  90f,  72f),
        new("loungeChair",    new Vector2(156f, 336f), 180f,  52f),
        new("loungeChair",    new Vector2(156f, 544f),   0f,  52f),
        new("lampRoundFloor", new Vector2(84f, 344f),    0f,  82f, EmitsWarmGlow: true),

        // West & Entrance Decorative Plants
        new("pottedPlant",    new Vector2(80f, 232f),   20f,  60f),
        new("pottedPlant",    new Vector2(272f, 220f), -15f,  58f),
        new("plantSmall1",    new Vector2(72f, 72f),     0f,  34f),

        // North Security Hall (side table back against North wall -> drawers face South 180)
        new("sideTableDrawers", new Vector2(640f, 52f), 180f, 60f),
        new("plantSmall1",    new Vector2(1208f, 72f),  45f,  34f),

        // Teller Station (employee chair South of counter -> faces North 0 toward monitor/glass)
        new("loungeChair",    new Vector2(560f, 604f),   0f,  52f),

        // East Waiting Lounge (sofa North-facing 0 toward coffee table)
        new("rugRectangle",   new Vector2(830f, 566f),   0f, 165f),
        new("loungeSofa",     new Vector2(830f, 646f),   0f, 105f),
        new("tableCoffee",    new Vector2(830f, 566f),   0f,  72f),
        new("lampRoundFloor", new Vector2(912f, 646f),   0f,  82f, EmitsWarmGlow: true),

        // East Corridor (side table back against East wall -> drawers face West +90)
        new("pottedPlant",      new Vector2(1200f, 688f), 30f, 60f),
        new("sideTableDrawers", new Vector2(1224f, 380f), 90f, 60f)
    };
}
