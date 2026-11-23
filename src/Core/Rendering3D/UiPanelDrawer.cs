using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Draws consistent rounded tactical HUD cards, status accent stripes, and corner briefing brackets.
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
    private const int BracketLength = 12;
    private const int BracketThickness = 2;
    private const int BracketOffset = 1;

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

    public static void DrawBriefingCorners(Rectangle bounds, Color bracketColor)
    {
        int left = (int)bounds.X - BracketOffset;
        int top = (int)bounds.Y - BracketOffset;
        int right = (int)(bounds.X + bounds.Width) + BracketOffset;
        int bottom = (int)(bounds.Y + bounds.Height) + BracketOffset;

        DrawCornerBracket(left, top, 1, 1, bracketColor);
        DrawCornerBracket(right, top, -1, 1, bracketColor);
        DrawCornerBracket(left, bottom, 1, -1, bracketColor);
        DrawCornerBracket(right, bottom, -1, -1, bracketColor);
    }

    private static void DrawCornerBracket(int cornerX, int cornerY, int dirX, int dirY, Color color)
    {
        int horizX = dirX > 0 ? cornerX : cornerX - BracketLength;
        int horizY = dirY > 0 ? cornerY : cornerY - BracketThickness;
        Raylib.DrawRectangle(horizX, horizY, BracketLength, BracketThickness, color);

        int vertX = dirX > 0 ? cornerX : cornerX - BracketThickness;
        int vertY = dirY > 0 ? cornerY : cornerY - BracketLength;
        Raylib.DrawRectangle(vertX, vertY, BracketThickness, BracketLength, color);
    }
}
