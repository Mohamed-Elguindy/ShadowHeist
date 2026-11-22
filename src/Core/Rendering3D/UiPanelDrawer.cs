using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Draws consistent rounded tactical HUD cards and optional status accent stripes.
/// </summary>
public static class UiPanelDrawer
{
    private const float CardRoundness = 0.16f;
    private const int CardSegments = 8;
    private const float BorderThickness = 1.25f;
    private const int AccentInsetX = 2;
    private const int AccentInsetY = 5;
    private const int AccentWidth = 3;
    private const int AccentBothSides = 2;

    public static void DrawTacticalCard(Rectangle bounds, Color background, Color border, Color? leftAccent = null)
    {
        Raylib.DrawRectangleRounded(bounds, CardRoundness, CardSegments, background);
        Raylib.DrawRectangleRoundedLines(bounds, CardRoundness, CardSegments, BorderThickness, border);

        if (leftAccent is Color accentColor)
        {
            int stripeHeight = Math.Max(1, (int)bounds.Height - (AccentInsetY * AccentBothSides));
            Raylib.DrawRectangle((int)bounds.X + AccentInsetX, (int)bounds.Y + AccentInsetY, AccentWidth, stripeHeight, accentColor);
        }
    }
}
