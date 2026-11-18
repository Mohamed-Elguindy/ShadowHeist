using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders tactical screen-space HUD banners, sound meter, 3D-projected hold rings &amp; prompts, and outcome overlays.
/// </summary>
public sealed class HudRenderer
{
    private const int BannerFontSize = 20;
    private const int BannerBoxHeight = 36;
    private const int BannerHorizontalPadding = 28;
    private const int BannerTextOffsetX = 14;
    private const int BannerTextOffsetY = 8;
    private const int HintFontSize = 18;
    private const int HudMargin = 20;
    private const int VignetteBandSize = 64;
    private const float HoldRingHeightOverGround = 42f;
    private const float PromptHeightOverGround = 88f;

    private static readonly Color HintTextColor = new(110, 225, 255, 255);
    private static readonly Color VignetteEdge = new(4, 6, 12, 130);
    private static readonly Color AlarmBoxBackground = new(48, 12, 22, 225);
    private static readonly Color AlarmBoxBorder = new(244, 63, 94, 235);
    private static readonly Color AlarmTextColor = new(255, 110, 130, 255);
    private static readonly Color CashBoxBackground = new(15, 23, 42, 220);
    private static readonly Color CashBoxBorder = new(56, 189, 248, 180);

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
        SummaryScreen.Draw(context);
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

    private static void DrawAlarmBanner(bool isAlarmTriggered, HeistOutcome outcome)
    {
        if (!isAlarmTriggered || outcome != HeistOutcome.Playing)
        {
            return;
        }

        const string text = "! ALARM TRIGGERED !";
        int textWidth = Raylib.MeasureText(text, BannerFontSize);
        int boxWidth = textWidth + BannerHorizontalPadding;
        int boxX = (Raylib.GetScreenWidth() - boxWidth) / 2;

        Raylib.DrawRectangle(boxX, HudMargin, boxWidth, BannerBoxHeight, AlarmBoxBackground);
        Raylib.DrawRectangleLines(boxX, HudMargin, boxWidth, BannerBoxHeight, AlarmBoxBorder);
        Raylib.DrawText(text, boxX + BannerTextOffsetX, HudMargin + BannerTextOffsetY, BannerFontSize, AlarmTextColor);
    }

    private static void DrawStolenCashBanner(int stolenCash)
    {
        string text = $"STOLEN CASH: ${stolenCash:N0} / ${HeistConstants.TargetCashGoal:N0}";
        int textWidth = Raylib.MeasureText(text, BannerFontSize);
        int boxWidth = textWidth + BannerHorizontalPadding;
        int boxX = Raylib.GetScreenWidth() - boxWidth - HudMargin;

        Raylib.DrawRectangle(boxX, HudMargin, boxWidth, BannerBoxHeight, CashBoxBackground);
        Raylib.DrawRectangleLines(boxX, HudMargin, boxWidth, BannerBoxHeight, CashBoxBorder);

        Color textColor = stolenCash >= HeistConstants.TargetCashGoal ? Color.Gold : Color.Green;
        Raylib.DrawText(text, boxX + BannerTextOffsetX, HudMargin + BannerTextOffsetY, BannerFontSize, textColor);
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
        if (activeHold is null)
        {
            return;
        }

        var worldPos = new Vector3(activeHold.Position.X, HoldRingHeightOverGround, activeHold.Position.Y);
        if (TryProjectInFrontOfCamera(worldPos, context.Camera.Camera, out Vector2 screenPosition))
        {
            HoldProgressRingRenderer.Draw(screenPosition, activeHold.HoldProgress);
        }
    }

    private static void DrawContextualPrompt(GameContext context)
    {
        IInteractable? activeItem = InteractableQuery.FindInPlayerRange(context);
        if (activeItem is null || string.IsNullOrWhiteSpace(activeItem.PromptText))
        {
            return;
        }

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
