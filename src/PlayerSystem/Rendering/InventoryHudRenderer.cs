using System.Globalization;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders a compact HUD pill showing carried loot weight and movement speed penalty when loot is carried.
/// </summary>
public static class InventoryHudRenderer
{
    private const int MarginLeft = 20;
    private const int MarginTop = 108 + GameWindowChrome.HeaderHeight;
    private const int BoxHeight = 32;
    private const int HorizontalPadding = 12;
    private const int FontSize = 13;
    private const int CenterDivisor = 2;
    private const float PercentScale = 100f;
    private const float FullSpeedMultiplier = 1f;

    private static readonly Color PanelBackground = new(15, 23, 42, 224);
    private static readonly Color PanelBorder = new(245, 158, 11, 190);
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
        int textWidth = UiTypography.MeasureTextWidth(text, FontSize, bold: true);
        int boxWidth = textWidth + (HorizontalPadding * CenterDivisor);

        UiPanelDrawer.DrawTacticalCard(new Rectangle(MarginLeft, MarginTop, boxWidth, BoxHeight), PanelBackground, PanelBorder, PanelBorder);

        int textY = MarginTop + ((BoxHeight - FontSize) / CenterDivisor);
        UiTypography.DrawText(text, MarginLeft + HorizontalPadding, textY, FontSize, LabelColor, bold: true);
    }
}
