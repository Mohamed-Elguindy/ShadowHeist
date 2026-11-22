using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Screen-space prompt box renderer for nearby world interactables.
/// </summary>
public static class HudPromptRenderer
{
    private const int DefaultFontSize = 16;
    private const int HorizontalPadding = 20;
    private const int VerticalPadding = 10;
    private const int ScreenEdgeMargin = 8;
    private const int MinScreenTop = ScreenEdgeMargin + GameWindowChrome.HeaderHeight;

    private static readonly Color BoxBackground = new(0, 0, 0, 210);
    private static readonly Color BoxBorder = Color.White;
    private static readonly Color PromptTextColor = Color.Yellow;

    public static void DrawPromptBox(string prompt, Vector2 anchorScreenPosition, float yOffset = 0f)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return;
        }

        int textWidth = Raylib.MeasureText(prompt, DefaultFontSize);
        int boxWidth = textWidth + HorizontalPadding;
        int boxHeight = DefaultFontSize + VerticalPadding;

        int maxX = Math.Max(ScreenEdgeMargin, Raylib.GetScreenWidth() - boxWidth - ScreenEdgeMargin);
        int maxY = Math.Max(MinScreenTop, Raylib.GetScreenHeight() - boxHeight - ScreenEdgeMargin);

        int boxX = Math.Clamp((int)anchorScreenPosition.X - (boxWidth / 2), ScreenEdgeMargin, maxX);
        int boxY = Math.Clamp((int)(anchorScreenPosition.Y + yOffset), MinScreenTop, maxY);

        Raylib.DrawRectangle(boxX, boxY, boxWidth, boxHeight, BoxBackground);
        Raylib.DrawRectangleLines(boxX, boxY, boxWidth, boxHeight, BoxBorder);
        Raylib.DrawText(prompt, boxX + (HorizontalPadding / 2), boxY + (VerticalPadding / 2), DefaultFontSize, PromptTextColor);
    }

    public static bool TryProjectInFrontOfCamera(Vector3 worldPos, Camera3D camera, out Vector2 screenPos)
    {
        Vector3 forward = camera.Target - camera.Position;
        if (Vector3.Dot(worldPos - camera.Position, forward) <= 0f)
        {
            screenPos = Vector2.Zero;
            return false;
        }

        screenPos = Raylib.GetWorldToScreen(worldPos, camera);
        return true;
    }
}
