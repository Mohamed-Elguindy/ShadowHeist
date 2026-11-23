using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Screen-space prompt box renderer for nearby world interactables.
/// </summary>
public static class HudPromptRenderer
{
    private const int DefaultFontSize = 15;
    private const int HorizontalPadding = 22;
    private const int VerticalPadding = 12;
    private const int ScreenEdgeMargin = 8;
    private const int MinScreenTop = ScreenEdgeMargin + GameWindowChrome.HeaderHeight;
    private const int CenterDivisor = 2;

    private static readonly Color BoxBackground = new(10, 15, 28, 228);
    private static readonly Color BoxBorder = new(250, 204, 21, 210);
    private static readonly Color PromptTextColor = new(254, 240, 138, 255);

    public static void DrawPromptBox(string prompt, Vector2 anchorScreenPosition, float yOffset = 0f)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        int textWidth = UiTypography.MeasureTextWidth(prompt, DefaultFontSize, bold: true);
        int boxWidth = textWidth + HorizontalPadding;
        int boxHeight = DefaultFontSize + VerticalPadding;

        int maxX = Math.Max(ScreenEdgeMargin, Raylib.GetScreenWidth() - boxWidth - ScreenEdgeMargin);
        int maxY = Math.Max(MinScreenTop, Raylib.GetScreenHeight() - boxHeight - ScreenEdgeMargin);

        int boxX = Math.Clamp((int)anchorScreenPosition.X - (boxWidth / CenterDivisor), ScreenEdgeMargin, maxX);
        int boxY = Math.Clamp((int)(anchorScreenPosition.Y + yOffset), MinScreenTop, maxY);

        UiPanelDrawer.DrawTacticalCard(new Rectangle(boxX, boxY, boxWidth, boxHeight), BoxBackground, BoxBorder, BoxBorder);
        UiTypography.DrawText(prompt, boxX + (HorizontalPadding / CenterDivisor), boxY + (VerticalPadding / CenterDivisor), DefaultFontSize, PromptTextColor, bold: true);
    }

    public static bool TryProjectInFrontOfCamera(Vector3 worldPos, Camera3D camera, out Vector2 screenPos)
    {
        Vector3 forward = camera.Target - camera.Position;
        if (Vector3.Dot(worldPos - camera.Position, forward) <= 0f)
        {
            screenPos = default;
            return false;
        }

        screenPos = Raylib.GetWorldToScreen(worldPos, camera);
        return true;
    }
}
