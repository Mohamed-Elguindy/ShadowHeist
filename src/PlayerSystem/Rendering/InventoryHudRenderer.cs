using System.Globalization;
using Raylib_cs;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders a compact HUD pill showing carried loot weight and movement speed penalty when loot is carried.
/// </summary>
public static class InventoryHudRenderer
{
    private const int MarginLeft = 20;
    private const int MarginTop = 108;
    private const int BoxHeight = 32;
    private const int HorizontalPadding = 10;
    private const int FontSize = 14;
    private const float PercentScale = 100f;
    private const float FullSpeedMultiplier = 1f;

    private static readonly Color PanelBackground = new(15, 23, 42, 220);
    private static readonly Color PanelBorder = new(245, 158, 11, 200);
    private static readonly Color LabelColor = new(253, 224, 71, 255);

    public static void Draw(int itemCount, float totalWeightKg, float speedMultiplier)
    {
        if (itemCount <= 0)
        {
            return;
        }

        int slowdownPercent = Math.Max(0, (int)MathF.Round((FullSpeedMultiplier - speedMultiplier) * PercentScale));
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"BAG: {itemCount} ({totalWeightKg:0.0} kg | -{slowdownPercent}% SPD)");
        int textWidth = Raylib.MeasureText(text, FontSize);
        int boxWidth = textWidth + (HorizontalPadding * 2);

        Raylib.DrawRectangle(MarginLeft, MarginTop, boxWidth, BoxHeight, PanelBackground);
        Raylib.DrawRectangleLines(MarginLeft, MarginTop, boxWidth, BoxHeight, PanelBorder);

        int textY = MarginTop + ((BoxHeight - FontSize) / 2);
        Raylib.DrawText(text, MarginLeft + HorizontalPadding, textY, FontSize, LabelColor);
    }
}
