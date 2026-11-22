using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders tactical screen-space HUD banners, sound meter, extraction cues, menus, and mission summary.
/// </summary>
public sealed class HudRenderer
{
    private const int BannerFontSize = 16;
    private const int BannerBoxHeight = 34;
    private const int BannerHorizontalPadding = 32;
    private const int BannerTextOffsetX = 16;
    private const int BannerTextOffsetY = 9;
    private const int HintFontSize = 13;
    private const int HintPillPaddingX = 10;
    private const int HintPillHeight = 24;
    private const int HintPillGap = 8;
    private const int HintBottomOffset = 34;
    private const int HudMargin = 20;
    private const int TopBannerMargin = HudMargin + GameWindowChrome.HeaderHeight;
    private const int VignetteBandSize = 68;
    private const int CenterDivisor = 2;
    private const float AlarmVignetteSpeed = 4.5f;
    private const float AlarmVignetteBaseAlpha = 75f;
    private const float AlarmVignettePulseAlpha = 55f;
    private const float CaughtImpactFlashMaxAlpha = 72f;
    private const float HoldRingHeightOverGround = 42f;
    private const float PromptHeightOverGround = 88f;

    private static readonly Color HintPillBackground = new(12, 18, 32, 200);
    private static readonly Color HintPillBorder = new(51, 65, 85, 190);
    private static readonly Color HintTextColor = new(148, 163, 184, 255);
    private static readonly Color VignetteEdge = new(4, 6, 12, 130);
    private static readonly Color AlarmVignetteRgb = new(185, 28, 56, 0);
    private static readonly Color AlarmBoxBackground = new(48, 12, 22, 230);
    private static readonly Color AlarmBoxBorder = new(244, 63, 94, 235);
    private static readonly Color AlarmTextColor = new(255, 110, 130, 255);

    private readonly SoundMeter _soundMeter;
    private readonly RectangleBounds _getawayZone;

    public HudRenderer(SoundMeter soundMeter, RectangleBounds? getawayZone = null)
    {
        _soundMeter = soundMeter;
        _getawayZone = getawayZone ?? HeistConstants.DefaultGetawayZone;
    }

    public void DrawHudOverlay(GameContext context)
    {
        bool showCrimsonVignette = (context.IsAlarmTriggered && context.Outcome == HeistOutcome.Playing) || context.Cutscene.IsCaughtPlaying;
        DrawVignette(showCrimsonVignette, context.ElapsedSeconds, context.Cutscene.ResolveCaughtRedPulseIntensity());

        GameFlowState flowState = context.StateManager.CurrentState;
        if (flowState is GameFlowState.MainMenu or GameFlowState.LevelSelect)
        {
            MenuOverlayRenderer.Draw(context);
            return;
        }

        if (!context.Cutscene.IsActive)
        {
            DrawGameplayHud(context);
        }

        if (flowState == GameFlowState.Paused)
        {
            MenuOverlayRenderer.Draw(context);
            return;
        }

        MissionIntroOverlay.Draw(context);
        SummaryScreen.Draw(context);
    }

    private void DrawGameplayHud(GameContext context)
    {
        SoundMeterBarRenderer.Draw(_soundMeter.Level);
        DrawAlarmBanner(context.IsAlarmTriggered, context.Outcome);
        MissionTelemetryHud.DrawTopRightTelemetry(context.StolenCash, context.ElapsedSeconds, context.IsPlayerSneaking);
        ExtractionHudRenderer.Draw(context, _getawayZone);
        DrawHintBar(context.Camera.IsOverviewMode);
        DrawHoldProgressRing(context);
        DrawContextualPrompt(context);
    }

    private static void DrawVignette(bool isAlarmActive, float elapsedSeconds, float caughtPulseIntensity)
    {
        int width = Raylib.GetScreenWidth();
        int height = Raylib.GetScreenHeight();
        Color transparent = new(0, 0, 0, 0);
        Color edgeColor = ResolveVignetteColor(isAlarmActive, elapsedSeconds, caughtPulseIntensity);

        if (caughtPulseIntensity > 0f)
        {
            byte flashAlpha = (byte)(CaughtImpactFlashMaxAlpha * caughtPulseIntensity);
            Raylib.DrawRectangle(0, GameWindowChrome.HeaderHeight, width, height - GameWindowChrome.HeaderHeight, new Color(AlarmVignetteRgb.R, AlarmVignetteRgb.G, AlarmVignetteRgb.B, flashAlpha));
        }

        Raylib.DrawRectangleGradientV(0, 0, width, VignetteBandSize, edgeColor, transparent);
        Raylib.DrawRectangleGradientV(0, height - VignetteBandSize, width, VignetteBandSize, transparent, edgeColor);
        Raylib.DrawRectangleGradientH(0, 0, VignetteBandSize, height, edgeColor, transparent);
        Raylib.DrawRectangleGradientH(width - VignetteBandSize, 0, VignetteBandSize, height, transparent, edgeColor);
    }

    private static Color ResolveVignetteColor(bool isAlarmActive, float elapsedSeconds, float caughtPulseIntensity)
    {
        if (!isAlarmActive)
        {
            return VignetteEdge;
        }

        float pulseWeight = caughtPulseIntensity > 0f
            ? MathF.Max(0.5f + 0.5f * MathF.Sin(elapsedSeconds * AlarmVignetteSpeed), caughtPulseIntensity)
            : 0.5f + 0.5f * MathF.Sin(elapsedSeconds * AlarmVignetteSpeed);
        byte alpha = (byte)(AlarmVignetteBaseAlpha + (AlarmVignettePulseAlpha * pulseWeight));
        return new Color(AlarmVignetteRgb.R, AlarmVignetteRgb.G, AlarmVignetteRgb.B, alpha);
    }

    private static void DrawAlarmBanner(bool isAlarmTriggered, HeistOutcome outcome)
    {
        if (!isAlarmTriggered || outcome != HeistOutcome.Playing)
        {
            return;
        }

        const string text = "! ALARM TRIGGERED !";
        int textWidth = UiTypography.MeasureTextWidth(text, BannerFontSize, bold: true);
        int boxWidth = textWidth + BannerHorizontalPadding;
        int boxX = (Raylib.GetScreenWidth() - boxWidth) / CenterDivisor;

        UiPanelDrawer.DrawTacticalCard(new Rectangle(boxX, TopBannerMargin, boxWidth, BannerBoxHeight), AlarmBoxBackground, AlarmBoxBorder, AlarmBoxBorder);
        UiTypography.DrawText(text, boxX + BannerTextOffsetX, TopBannerMargin + BannerTextOffsetY, BannerFontSize, AlarmTextColor, bold: true);
    }

    private static void DrawHintBar(bool isOverviewCamera)
    {
        string cameraLabel = isOverviewCamera ? "FOLLOW" : "OVERVIEW";
        string[] pills = { $"[F6] {cameraLabel}", "[RMB] PEEK", "[SCROLL] ZOOM", "[R] RESTART", "[F11] DISPLAY", "[ESC] PAUSE" };

        int x = HudMargin;
        int y = Raylib.GetScreenHeight() - HintBottomOffset;
        int textOffsetY = (HintPillHeight - HintFontSize) / CenterDivisor;

        foreach (string pill in pills)
        {
            int pillWidth = UiTypography.MeasureTextWidth(pill, HintFontSize) + (HintPillPaddingX * CenterDivisor);
            UiPanelDrawer.DrawTacticalCard(new Rectangle(x, y, pillWidth, HintPillHeight), HintPillBackground, HintPillBorder);
            UiTypography.DrawText(pill, x + HintPillPaddingX, y + textOffsetY, HintFontSize, HintTextColor);
            x += pillWidth + HintPillGap;
        }
    }

    private static void DrawHoldProgressRing(GameContext context)
    {
        IHoldInteractable? activeHold = InteractableQuery.FindHoldInProgress(context);
        if (activeHold is null)
        {
            return;
        }

        var worldPos = new Vector3(activeHold.Position.X, HoldRingHeightOverGround, activeHold.Position.Y);
        if (HudPromptRenderer.TryProjectInFrontOfCamera(worldPos, context.Camera.Camera, out Vector2 screenPosition))
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
        if (HudPromptRenderer.TryProjectInFrontOfCamera(worldPos, context.Camera.Camera, out Vector2 screenPosition))
        {
            HudPromptRenderer.DrawPromptBox(activeItem.PromptText, screenPosition);
        }
    }
}
