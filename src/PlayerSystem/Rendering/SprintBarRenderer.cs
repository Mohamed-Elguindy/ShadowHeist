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
    private const int BarHeight = 12;
    private const int MarginLeft = 20;
    private const int MarginTop = 64 + GameWindowChrome.HeaderHeight;
    private const int BoxHeight = 36;
    private const int Padding = 8;
    private const int Spacing = 10;

    private static readonly Color PanelBackground = new(15, 23, 42, 220);
    private static readonly Color PanelBorder = new(56, 189, 248, 180);
    private static readonly Color TrackBackground = new(30, 41, 59, 255);
    private static readonly Color TrackBorder = new(71, 85, 105, 255);
    private static readonly Color StaminaBlue = new(56, 189, 248, 255);
    private static readonly Color CooldownColor = new(100, 116, 139, 255);

    public static void Draw(float staminaRatio, bool isOnCooldown)
    {
        float clamped = Math.Clamp(staminaRatio, 0f, 1f);
        int fillWidth = (int)(BarWidth * clamped);
        int labelWidth = Raylib.MeasureText(LabelText, LabelFontSize);
        int boxWidth = Padding + labelWidth + Spacing + BarWidth + Padding;

        Raylib.DrawRectangle(MarginLeft, MarginTop, boxWidth, BoxHeight, PanelBackground);
        Raylib.DrawRectangleLines(MarginLeft, MarginTop, boxWidth, BoxHeight, isOnCooldown ? CooldownColor : PanelBorder);

        int labelY = MarginTop + (BoxHeight - LabelFontSize) / 2;
        Raylib.DrawText(LabelText, MarginLeft + Padding, labelY, LabelFontSize, isOnCooldown ? CooldownColor : Color.RayWhite);

        int barX = MarginLeft + Padding + labelWidth + Spacing;
        int barY = MarginTop + (BoxHeight - BarHeight) / 2;
        Raylib.DrawRectangle(barX, barY, BarWidth, BarHeight, TrackBackground);
        if (fillWidth > 0)
        {
            Raylib.DrawRectangle(barX, barY, fillWidth, BarHeight, StaminaBlue);
        }

        Raylib.DrawRectangleLines(barX, barY, BarWidth, BarHeight, TrackBorder);
    }
}

