using Raylib_cs;

namespace ShadowHeist.HeistWorld.UI;

public static class SoundMeterBarRenderer
{
    private const string LabelText = "NOISE";
    private const int LabelFontSize = 14;
    private const int BarWidth = 120;
    private const int BarHeight = 12;
    private const int MarginLeft = 20;
    private const int MarginTop = 20;
    private const int BoxHeight = 36;
    private const int Padding = 8;
    private const int Spacing = 10;
    private const float QuietLevelLimit = 0.35f;
    private const float LoudLevelStart = 0.70f;

    private static readonly Color PanelBackground = new(15, 23, 42, 220);
    private static readonly Color PanelBorder = new(56, 189, 248, 180);
    private static readonly Color TrackBackground = new(30, 41, 59, 255);
    private static readonly Color TrackBorder = new(71, 85, 105, 255);
    private static readonly Color QuietColor = new(34, 197, 94, 255);
    private static readonly Color ModerateColor = new(234, 179, 8, 255);
    private static readonly Color LoudColor = new(239, 68, 68, 255);

    public static void Draw(float level)
    {
        if (level <= 0f)
        {
            return;
        }

        int labelWidth = Raylib.MeasureText(LabelText, LabelFontSize);
        int boxWidth = Padding + labelWidth + Spacing + BarWidth + Padding;
        int boxX = MarginLeft;
        int boxY = MarginTop;

        Raylib.DrawRectangle(boxX, boxY, boxWidth, BoxHeight, PanelBackground);
        Raylib.DrawRectangleLines(boxX, boxY, boxWidth, BoxHeight, PanelBorder);

        int labelX = boxX + Padding;
        int labelY = boxY + (BoxHeight - LabelFontSize) / 2;
        Raylib.DrawText(LabelText, labelX, labelY, LabelFontSize, Color.RayWhite);

        int barX = labelX + labelWidth + Spacing;
        int barY = boxY + (BoxHeight - BarHeight) / 2;
        Raylib.DrawRectangle(barX, barY, BarWidth, BarHeight, TrackBackground);

        float clampedLevel = Math.Clamp(level, 0f, 1f);
        int fillWidth = (int)(BarWidth * clampedLevel);
        if (fillWidth > 0)
        {
            Raylib.DrawRectangle(barX, barY, fillWidth, BarHeight, ResolveFillColor(clampedLevel));
        }

        Raylib.DrawRectangleLines(barX, barY, BarWidth, BarHeight, TrackBorder);
    }

    private static Color ResolveFillColor(float level)
    {
        if (level >= LoudLevelStart)
        {
            return LoudColor;
        }

        if (level >= QuietLevelLimit)
        {
            return ModerateColor;
        }

        return QuietColor;
    }
}
