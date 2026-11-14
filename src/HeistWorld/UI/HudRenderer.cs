using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.UI;

public sealed class HudRenderer
{
    private const int CashFontSize = 20;

    private readonly SoundMeter _soundMeter;

    public HudRenderer(SoundMeter soundMeter)
    {
        _soundMeter = soundMeter;
    }

    public void DrawHudOverlay(GameContext context)
    {
        DrawStolenCashBanner(context.StolenCash);
        DrawAlarmBanner(context.IsAlarmTriggered, context.Outcome);
        DrawSoundMeter();
        DrawHoldProgressRing(context);
        DrawContextualPrompt(context);
        DrawOutcomeOverlay(context.Outcome);
    }

    public static void DrawAlarmBanner(bool isAlarmTriggered, HeistOutcome outcome)
    {
        if (!isAlarmTriggered || outcome != HeistOutcome.Playing) return;

        const string text = "! ALARM TRIGGERED !";
        const int fontSize = 20;
        int textWidth = Raylib.MeasureText(text, fontSize);
        int boxWidth = textWidth + 28;
        const int boxHeight = 36;
        int boxX = (Raylib.GetScreenWidth() - boxWidth) / 2;
        const int boxY = 20;

        Raylib.DrawRectangle(boxX, boxY, boxWidth, boxHeight, new Color(48, 12, 22, 225));
        Raylib.DrawRectangleLines(boxX, boxY, boxWidth, boxHeight, new Color(244, 63, 94, 235));
        Raylib.DrawText(text, boxX + 14, boxY + 8, fontSize, new Color(255, 110, 130, 255));
    }

    public static void DrawStolenCashBanner(int stolenCash)
    {
        string text = $"STOLEN CASH: ${stolenCash:N0} / ${HeistConstants.TargetCashGoal:N0}";
        int textWidth = Raylib.MeasureText(text, CashFontSize);
        int boxWidth = textWidth + 28;
        const int boxHeight = 36;
        int boxX = Raylib.GetScreenWidth() - boxWidth - 20;
        const int boxY = 20;

        Raylib.DrawRectangle(boxX, boxY, boxWidth, boxHeight, new Color(15, 23, 42, 220));
        Raylib.DrawRectangleLines(boxX, boxY, boxWidth, boxHeight, new Color(56, 189, 248, 180));

        Color textColor = stolenCash >= HeistConstants.TargetCashGoal ? Color.Gold : Color.Green;
        Raylib.DrawText(text, boxX + 14, boxY + 8, CashFontSize, textColor);
    }

    public static void DrawOutcomeOverlay(HeistOutcome outcome)
    {
        if (outcome == HeistOutcome.Playing) return;

        bool won = outcome == HeistOutcome.Escaped;
        string title = won ? "HEIST COMPLETE — ESCAPED!" : "BUSTED — CAUGHT BY SECURITY!";
        const string subtitle = "Press [R] to Restart Mission";
        Color accent = won ? new Color(74, 222, 128, 255) : new Color(244, 63, 94, 255);

        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        int titleWidth = Raylib.MeasureText(title, 34);
        int subtitleWidth = Raylib.MeasureText(subtitle, 20);
        int panelWidth = Math.Max(titleWidth, subtitleWidth) + 64;
        const int panelHeight = 110;
        int panelX = (screenWidth - panelWidth) / 2;
        int panelY = (screenHeight - panelHeight) / 2;

        Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, new Color(10, 14, 24, 235));
        Raylib.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, accent);
        Raylib.DrawText(title, (screenWidth - titleWidth) / 2, panelY + 22, 34, accent);
        Raylib.DrawText(subtitle, (screenWidth - subtitleWidth) / 2, panelY + 68, 20, Color.RayWhite);
    }

    private void DrawSoundMeter() =>
        SoundMeterBarRenderer.Draw(_soundMeter.Level);

    private static void DrawHoldProgressRing(GameContext context)
    {
        IHoldInteractable? activeHold = InteractableQuery.FindHoldInProgress(context);
        if (activeHold == null)
        {
            return;
        }

        HoldProgressRingRenderer.Draw(activeHold.Position, activeHold.HoldProgress);
    }

    private static void DrawContextualPrompt(GameContext context)
    {
        IInteractable? activeItem = InteractableQuery.FindInPlayerRange(context);
        if (activeItem == null || string.IsNullOrWhiteSpace(activeItem.PromptText))
        {
            return;
        }

        HudPromptRenderer.DrawPromptBox(activeItem.PromptText, activeItem.Position, yOffset: -50f);
    }
}
