using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders tactical 3D screen-space HUD banners, sound meter, 3D-projected hold rings & prompts, and outcome overlays.
/// </summary>
public sealed class HudRenderer
{
    private const int CashFontSize = 20;
    private const int HintFontSize = 18;
    private const int HudMargin = 20;
    private const int VignetteBandSize = 64;
    private const float HoldRingHeightOverGround = 42f;
    private const float PromptHeightOverGround = 88f;

    private static readonly Color HintTextColor = new(110, 225, 255, 255);
    private static readonly Color VignetteEdge = new(4, 6, 12, 130);

    private readonly SoundMeter _soundMeter;

    public HudRenderer(SoundMeter soundMeter)
    {
        _soundMeter = soundMeter;
    }

    public void DrawHudOverlay(GameContext context)
    {
        DrawVignette();
        SoundMeterBarRenderer.Draw(_soundMeter.Level);
        DrawAlarmBanner(context.IsAlarmTriggered, context.Outcome);
        DrawStolenCashBanner(context.StolenCash);
        DrawHintBar(context.Camera.IsOverviewMode);
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

    private static void DrawVignette()
    {
        int width = Raylib.GetScreenWidth();
        int height = Raylib.GetScreenHeight();
        Color transparent = new(0, 0, 0, 0);

        Raylib.DrawRectangleGradientV(0, 0, width, VignetteBandSize, VignetteEdge, transparent);
        Raylib.DrawRectangleGradientV(0, height - VignetteBandSize, width, VignetteBandSize, transparent, VignetteEdge);
        Raylib.DrawRectangleGradientH(0, 0, VignetteBandSize, height, VignetteEdge, transparent);
        Raylib.DrawRectangleGradientH(width - VignetteBandSize, 0, VignetteBandSize, height, transparent, VignetteEdge);
    }

    private static void DrawHintBar(bool isOverviewCamera)
    {
        string cameraLabel = isOverviewCamera ? "FOLLOW" : "OVERVIEW";
        string text = $"[F6] {cameraLabel}   |   [SCROLL] ZOOM   |   [R] RESTART";
        Raylib.DrawText(text, HudMargin, Raylib.GetScreenHeight() - 30, HintFontSize, HintTextColor);
    }

    private static void DrawHoldProgressRing(GameContext context)
    {
        IHoldInteractable? activeHold = InteractableQuery.FindHoldInProgress(context);
        if (activeHold is null) return;

        var worldPos = new Vector3(activeHold.Position.X, HoldRingHeightOverGround, activeHold.Position.Y);
        if (TryProjectInFrontOfCamera(worldPos, context.Camera.Camera, out Vector2 screenPosition))
        {
            HoldProgressRingRenderer.Draw(screenPosition, activeHold.HoldProgress);
        }
    }

    private static void DrawContextualPrompt(GameContext context)
    {
        IInteractable? activeItem = InteractableQuery.FindInPlayerRange(context);
        if (activeItem is null || string.IsNullOrWhiteSpace(activeItem.PromptText)) return;

        var worldPos = new Vector3(activeItem.Position.X, PromptHeightOverGround, activeItem.Position.Y);
        if (TryProjectInFrontOfCamera(worldPos, context.Camera.Camera, out Vector2 screenPosition))
        {
            HudPromptRenderer.DrawPromptBox(activeItem.PromptText, screenPosition);
        }
    }

    private static bool TryProjectInFrontOfCamera(Vector3 worldPos, Camera3D camera, out Vector2 screenPos)
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
