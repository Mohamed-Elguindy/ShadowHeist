using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.HeistWorld.UI;

public sealed class HudRenderer
{
    private const int TargetHeistGoal = 500_000;
    private const int CashFontSize = 20;
    private const int PromptFontSize = 16;

    public void DrawHudOverlay(GameContext context)
    {
        DrawStolenCashBanner(context.StolenCash);
        DrawContextualPrompt(context);
        DrawOutcomeOverlay(context.Outcome);
    }

    public static void DrawStolenCashBanner(int stolenCash)
    {
        string text = $"STOLEN CASH: ${stolenCash:N0} / ${TargetHeistGoal:N0}";
        int textWidth = Raylib.MeasureText(text, CashFontSize);

        int boxWidth = textWidth + 28;
        int boxHeight = 36;
        int boxX = Raylib.GetScreenWidth() - boxWidth - 20;
        const int boxY = 20;

        Raylib.DrawRectangle(boxX, boxY, boxWidth, boxHeight, new Color(15, 23, 42, 220));
        Raylib.DrawRectangleLines(boxX, boxY, boxWidth, boxHeight, new Color(56, 189, 248, 180));

        Color textColor = stolenCash >= TargetHeistGoal ? Color.Gold : Color.Green;
        Raylib.DrawText(text, boxX + 14, boxY + 8, CashFontSize, textColor);
    }

    public static void DrawOutcomeOverlay(HeistOutcome outcome)
    {
        if (outcome == HeistOutcome.Playing) return;

        bool won = outcome == HeistOutcome.Escaped;
        string title = won ? "HEIST COMPLETE — ESCAPED!" : "BUSTED — CAUGHT BY SECURITY!";
        string subtitle = "Press [R] to Restart Mission";
        Color accent = won ? new Color(74, 222, 128, 255) : new Color(244, 63, 94, 255);

        int w = Raylib.GetScreenWidth();
        int h = Raylib.GetScreenHeight();
        int titleW = Raylib.MeasureText(title, 34);
        int subW = Raylib.MeasureText(subtitle, 20);
        int panelW = Math.Max(titleW, subW) + 64;
        int panelH = 110;
        int px = (w - panelW) / 2;
        int py = (h - panelH) / 2;

        Raylib.DrawRectangle(px, py, panelW, panelH, new Color(10, 14, 24, 235));
        Raylib.DrawRectangleLines(px, py, panelW, panelH, accent);
        Raylib.DrawText(title, (w - titleW) / 2, py + 22, 34, accent);
        Raylib.DrawText(subtitle, (w - subW) / 2, py + 68, 20, Color.RayWhite);
    }

    private static void DrawContextualPrompt(GameContext context)
    {
        IInteractable? activeItem = InteractableQuery.FindInPlayerRange(context);
        if (activeItem == null || string.IsNullOrWhiteSpace(activeItem.PromptText))
        {
            return;
        }

        DrawPromptBox(activeItem.PromptText, activeItem.Position);
    }

    private static void DrawPromptBox(string prompt, Vector2 worldPosition)
    {
        int textWidth = Raylib.MeasureText(prompt, PromptFontSize);
        int boxW = textWidth + 20;
        int boxH = PromptFontSize + 10;
        int boxX = Math.Clamp((int)worldPosition.X - boxW / 2, 8, Math.Max(8, Raylib.GetScreenWidth() - boxW - 8));
        int boxY = Math.Clamp((int)worldPosition.Y - 50, 8, Math.Max(8, Raylib.GetScreenHeight() - boxH - 8));

        Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(0, 0, 0, 200));
        Raylib.DrawRectangleLines(boxX, boxY, boxW, boxH, Color.White);
        Raylib.DrawText(prompt, boxX + 10, boxY + 5, PromptFontSize, Color.Yellow);
    }
}
