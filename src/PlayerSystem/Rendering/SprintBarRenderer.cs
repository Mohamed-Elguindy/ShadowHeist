using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders the player's blue sprint stamina bar and cooldown indicator in the HUD.
/// </summary>
public static class SprintBarRenderer
{
    private const string LabelText = "SPRINT";
    private const int LabelFontSize = 14;
    private const int BarWidth = 120;
    private const int BarHeight = 10;
    private const int MarginLeft = 20;
    private const int MarginTop = 64 + GameWindowChrome.HeaderHeight;
    private const int BoxHeight = 36;
    private const int Padding = 12;
    private const int Spacing = 10;
    private const int CenterDivisor = 2;

    private static readonly Color PanelBackground = new(15, 23, 42, 224);
    private static readonly Color PanelBorder = new(56, 189, 248, 160);
    private static readonly Color TrackBackground = new(30, 41, 59, 255);
    private static readonly Color TrackBorder = new(71, 85, 105, 255);
    private static readonly Color StaminaBlue = new(56, 189, 248, 255);
    private static readonly Color CooldownColor = new(100, 116, 139, 255);

    public static void Draw(float staminaRatio, bool isOnCooldown)
    {
        float clamped = Math.Clamp(staminaRatio, 0f, 1f);
        int fillWidth = (int)(BarWidth * clamped);
        int labelWidth = UiTypography.MeasureTextWidth(LabelText, LabelFontSize, bold: true);
        int boxWidth = Padding + labelWidth + Spacing + BarWidth + Padding;
        Color accent = isOnCooldown ? CooldownColor : StaminaBlue;

        UiPanelDrawer.DrawTacticalCard(new Rectangle(MarginLeft, MarginTop, boxWidth, BoxHeight), PanelBackground, isOnCooldown ? CooldownColor : PanelBorder, accent);

        int labelY = MarginTop + ((BoxHeight - LabelFontSize) / CenterDivisor);
        UiTypography.DrawText(LabelText, MarginLeft + Padding, labelY, LabelFontSize, isOnCooldown ? CooldownColor : Color.RayWhite, bold: true);

        int barX = MarginLeft + Padding + labelWidth + Spacing;
        int barY = MarginTop + ((BoxHeight - BarHeight) / CenterDivisor);
        Raylib.DrawRectangle(barX, barY, BarWidth, BarHeight, TrackBackground);
        if (fillWidth > 0)
        {
            Raylib.DrawRectangle(barX, barY, fillWidth, BarHeight, StaminaBlue);
        }

        Raylib.DrawRectangleLines(barX, barY, BarWidth, BarHeight, TrackBorder);
    }
}
