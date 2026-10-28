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
    }

    private static void DrawStolenCashBanner(int stolenCash)
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
        int promptX = (int)worldPosition.X - textWidth / 2;
        int promptY = (int)worldPosition.Y - 45;

        int boxX = promptX - 10;
        int boxY = promptY - 5;
        int boxW = textWidth + 20;
        int boxH = PromptFontSize + 10;

        Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(0, 0, 0, 200));
        Raylib.DrawRectangleLines(boxX, boxY, boxW, boxH, Color.White);
        Raylib.DrawText(prompt, promptX, promptY, PromptFontSize, Color.Yellow);
    }
}
