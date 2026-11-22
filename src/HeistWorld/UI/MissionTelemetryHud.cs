using System.Globalization;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders the top-right stolen-cash progress card and mission clock / stance telemetry pill.
/// </summary>
internal static class MissionTelemetryHud
{
    private const int BannerFontSize = 16;
    private const int BannerBoxHeight = 36;
    private const int BannerHorizontalPadding = 32;
    private const int BannerTextOffsetX = 16;
    private const int BannerTextOffsetY = 8;
    private const int ProgressBarHeight = 3;
    private const int ProgressInsetX = 6;
    private const int ProgressBottomMargin = 5;
    private const int HudMargin = 20;
    private const int TopBannerMargin = HudMargin + GameWindowChrome.HeaderHeight;
    private const int TelemetryGapY = 8;
    private const int TelemetryTopMargin = TopBannerMargin + BannerBoxHeight + TelemetryGapY;
    private const int TelemetryHeight = 28;
    private const int TelemetryFontSize = 13;
    private const int TelemetryPaddingX = 12;
    private const int SecondsPerMinute = 60;
    private const int CenterDivisor = 2;

    private static readonly Color CardBackground = new(15, 23, 42, 224);
    private static readonly Color CardBorder = new(56, 189, 248, 175);
    private static readonly Color ProgressTrackColor = new(30, 41, 59, 230);
    private static readonly Color SneakStanceColor = new(56, 225, 255, 255);
    private static readonly Color ActiveStanceColor = new(226, 232, 240, 255);

    public static void DrawTopRightTelemetry(int stolenCash, float elapsedSeconds, bool isPlayerSneaking)
    {
        DrawStolenCashCard(stolenCash);
        DrawClockAndStancePill(elapsedSeconds, isPlayerSneaking);
    }

    private static void DrawStolenCashCard(int stolenCash)
    {
        string text = $"STOLEN CASH: ${stolenCash:N0} / ${HeistConstants.TargetCashGoal:N0}";
        int textWidth = UiTypography.MeasureTextWidth(text, BannerFontSize, bold: true);
        int boxWidth = textWidth + BannerHorizontalPadding;
        int boxX = Raylib.GetScreenWidth() - boxWidth - HudMargin;
        Color accentColor = stolenCash >= HeistConstants.TargetCashGoal ? Color.Gold : Color.Green;

        UiPanelDrawer.DrawTacticalCard(new Rectangle(boxX, TopBannerMargin, boxWidth, BannerBoxHeight), CardBackground, CardBorder, accentColor);
        UiTypography.DrawText(text, boxX + BannerTextOffsetX, TopBannerMargin + BannerTextOffsetY, BannerFontSize, accentColor, bold: true);

        float ratio = Math.Clamp(stolenCash / (float)HeistConstants.TargetCashGoal, 0f, 1f);
        int trackWidth = boxWidth - (ProgressInsetX * CenterDivisor);
        int trackY = TopBannerMargin + BannerBoxHeight - ProgressBottomMargin;
        Raylib.DrawRectangle(boxX + ProgressInsetX, trackY, trackWidth, ProgressBarHeight, ProgressTrackColor);
        if (ratio > 0f)
        {
            Raylib.DrawRectangle(boxX + ProgressInsetX, trackY, (int)(trackWidth * ratio), ProgressBarHeight, accentColor);
        }
    }

    private static void DrawClockAndStancePill(float elapsedSeconds, bool isPlayerSneaking)
    {
        int totalSeconds = Math.Max(0, (int)elapsedSeconds);
        int minutes = totalSeconds / SecondsPerMinute;
        int seconds = totalSeconds % SecondsPerMinute;
        string stanceLabel = isPlayerSneaking ? "SNEAK" : "ACTIVE";
        string text = string.Create(CultureInfo.InvariantCulture, $"TIME {minutes:00}:{seconds:00}   |   STANCE: {stanceLabel}");

        int textWidth = UiTypography.MeasureTextWidth(text, TelemetryFontSize, bold: true);
        int boxWidth = textWidth + (TelemetryPaddingX * CenterDivisor);
        int boxX = Raylib.GetScreenWidth() - boxWidth - HudMargin;
        int textY = TelemetryTopMargin + ((TelemetryHeight - TelemetryFontSize) / CenterDivisor);
        Color accent = isPlayerSneaking ? SneakStanceColor : ActiveStanceColor;

        UiPanelDrawer.DrawTacticalCard(new Rectangle(boxX, TelemetryTopMargin, boxWidth, TelemetryHeight), CardBackground, CardBorder, accent);
        UiTypography.DrawText(text, boxX + TelemetryPaddingX, textY, TelemetryFontSize, accent, bold: true);
    }
}
