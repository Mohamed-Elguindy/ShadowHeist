using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders atmospheric vignette edges, tactical HUD banner, control hints, and 3D-projected prompts.
/// </summary>
public static class HudOverlay3D
{
    private const int TargetCashGoal = 500_000;
    private const int HudFontSize = 20;
    private const int HintFontSize = 18;
    private const int PromptFontSize = 16;
    private const int HudMargin = 20;
    private const int HudPaddingX = 14;
    private const int HudBoxHeight = 36;
    private const int VignetteBandSize = 64;
    private const float PromptHeightOverGround = 78f;

    private static readonly Color HudPanelColor = new(15, 23, 42, 220);
    private static readonly Color HudBorderColor = new(56, 189, 248, 180);
    private static readonly Color HintTextColor = new(110, 225, 255, 255);
    private static readonly Color VignetteEdge = new(4, 6, 12, 130);

    public static void DrawFullHud(GameContext context, Camera3D camera, bool isOverviewCamera)
    {
        DrawVignette();
        DrawCashBanner(context.StolenCash);
        string camLabel = isOverviewCamera ? "FOLLOW" : "OVERVIEW";
        DrawHintBar($"[F5] 2D VIEW   |   [F6] {camLabel}   |   [SCROLL] ZOOM");
        DrawInteractionPrompt(context, camera);
    }

    public static void DrawHintBar(string text) =>
        Raylib.DrawText(text, HudMargin, Raylib.GetScreenHeight() - 30, HintFontSize, HintTextColor);

    private static void DrawVignette()
    {
        int w = Raylib.GetScreenWidth();
        int h = Raylib.GetScreenHeight();
        Color clear = new(0, 0, 0, 0);
        Raylib.DrawRectangleGradientV(0, 0, w, VignetteBandSize, VignetteEdge, clear);
        Raylib.DrawRectangleGradientV(0, h - VignetteBandSize, w, VignetteBandSize, clear, VignetteEdge);
        Raylib.DrawRectangleGradientH(0, 0, VignetteBandSize, h, VignetteEdge, clear);
        Raylib.DrawRectangleGradientH(w - VignetteBandSize, 0, VignetteBandSize, h, clear, VignetteEdge);
    }

    private static void DrawCashBanner(int stolenCash)
    {
        string cash = $"STOLEN CASH: ${stolenCash:N0} / ${TargetCashGoal:N0}";
        int boxWidth = Raylib.MeasureText(cash, HudFontSize) + HudPaddingX * 2;
        int boxX = Raylib.GetScreenWidth() - boxWidth - HudMargin;

        Raylib.DrawRectangle(boxX, HudMargin, boxWidth, HudBoxHeight, HudPanelColor);
        Raylib.DrawRectangleLines(boxX, HudMargin, boxWidth, HudBoxHeight, HudBorderColor);
        Color textColor = stolenCash >= TargetCashGoal ? Color.Gold : Color.Green;
        Raylib.DrawText(cash, boxX + HudPaddingX, HudMargin + 8, HudFontSize, textColor);
    }

    private static void DrawInteractionPrompt(GameContext context, Camera3D camera)
    {
        IInteractable? active = InteractableQuery.FindInPlayerRange(context);
        if (active is null || string.IsNullOrWhiteSpace(active.PromptText))
        {
            return;
        }

        Vector2 screen = Raylib.GetWorldToScreen(
            new Vector3(active.Position.X, PromptHeightOverGround, active.Position.Y), camera);
        int textWidth = Raylib.MeasureText(active.PromptText, PromptFontSize);
        int px = (int)screen.X - textWidth / 2;
        int py = (int)screen.Y;

        Raylib.DrawRectangle(px - 10, py - 5, textWidth + 20, PromptFontSize + 10, new Color(0, 0, 0, 210));
        Raylib.DrawRectangleLines(px - 10, py - 5, textWidth + 20, PromptFontSize + 10, Color.White);
        Raylib.DrawText(active.PromptText, px, py, PromptFontSize, Color.Yellow);
    }
}
