using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

public static class SoundMeterBarRenderer
{
    private const string LabelText = "NOISE";
    private const int LabelFontSize = 14;
    private const int BarWidth = 120;
    private const int BarHeight = 10;
    private const int MarginLeft = 20;
    private const int MarginTop = 20 + GameWindowChrome.HeaderHeight;
    private const int BoxHeight = 36;
    private const int Padding = 12;
    private const int Spacing = 10;
    private const int CenterDivisor = 2;
    private const float QuietLevelLimit = 0.35f;
    private const float LoudLevelStart = 0.70f;

    private static readonly Color PanelBackground = new(15, 23, 42, 224);
    private static readonly Color PanelBorder = new(56, 189, 248, 160);
    private static readonly Color TrackBackground = new(30, 41, 59, 255);
    private static readonly Color TrackBorder = new(71, 85, 105, 255);
    private static readonly Color QuietColor = new(34, 197, 94, 255);
    private static readonly Color ModerateColor = new(234, 179, 8, 255);
    private static readonly Color LoudColor = new(239, 68, 68, 255);

    public static void Draw(float level)
    {
        float clampedLevel = Math.Clamp(level, 0f, 1f);
        int fillWidth = (int)(BarWidth * clampedLevel);
        int labelWidth = UiTypography.MeasureTextWidth(LabelText, LabelFontSize, bold: true);
        int boxWidth = Padding + labelWidth + Spacing + BarWidth + Padding;
        Color accent = ResolveFillColor(clampedLevel);

        UiPanelDrawer.DrawTacticalCard(new Rectangle(MarginLeft, MarginTop, boxWidth, BoxHeight), PanelBackground, PanelBorder, accent);
        DrawNoiseLabel(MarginLeft, MarginTop);
        DrawMeterTrackAndFill(MarginLeft, MarginTop, labelWidth, fillWidth, accent);
    }

    private static void DrawNoiseLabel(int boxX, int boxY)
    {
        int labelX = boxX + Padding;
        int labelY = boxY + ((BoxHeight - LabelFontSize) / CenterDivisor);
        UiTypography.DrawText(LabelText, labelX, labelY, LabelFontSize, Color.RayWhite, bold: true);
    }

    private static void DrawMeterTrackAndFill(int boxX, int boxY, int labelWidth, int fillWidth, Color fillColor)
    {
        int barX = boxX + Padding + labelWidth + Spacing;
        int barY = boxY + ((BoxHeight - BarHeight) / CenterDivisor);
        Raylib.DrawRectangle(barX, barY, BarWidth, BarHeight, TrackBackground);
        if (fillWidth > 0)
        {
            Raylib.DrawRectangle(barX, barY, fillWidth, BarHeight, fillColor);
        }

        Raylib.DrawRectangleLines(barX, barY, BarWidth, BarHeight, TrackBorder);
    }

    private static Color ResolveFillColor(float level)
    {
        if (level >= LoudLevelStart)
        {
            return LoudColor;
        }

        return level >= QuietLevelLimit ? ModerateColor : QuietColor;
    }
}
