using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders screen-space mission objective banners and projected extraction tags for GetawayVanZone.
/// </summary>
public static class ExtractionHudRenderer
{
    private const int BannerFontSize = 16;
    private const int BannerHeight = 34;
    private const int BannerHorizontalPadding = 32;
    private const int BannerTopDefaultY = 20 + GameWindowChrome.HeaderHeight;
    private const int BannerTopBelowAlarmY = 64 + GameWindowChrome.HeaderHeight;
    private const int CenterDivisor = 2;
    private const float MarkerWorldHeightY = 96f;
    private const string ExtractMarkerText = "GETAWAY VAN — EXTRACT HERE";

    private static readonly Color ObjectiveBgColor = new(15, 23, 42, 224);
    private static readonly Color ObjectiveBorderColor = new(56, 189, 248, 175);
    private static readonly Color ObjectiveTextColor = new(186, 230, 253, 255);
    private static readonly Color ReadyBgColor = new(12, 42, 28, 228);
    private static readonly Color ReadyBorderColor = new(74, 222, 128, 220);
    private static readonly Color ReadyTextColor = new(134, 239, 172, 255);

    public static void Draw(GameContext context, RectangleBounds getawayZone)
    {
        if (context.Outcome != HeistOutcome.Playing)
        {
            return;
        }

        bool isReady = context.StolenCash >= HeistConstants.TargetCashGoal;
        int bannerY = context.IsAlarmTriggered ? BannerTopBelowAlarmY : BannerTopDefaultY;
        DrawObjectiveBanner(isReady, bannerY);

        if (isReady)
        {
            DrawWorldExtractionTag(context.Camera.Camera, getawayZone);
        }
    }

    private static void DrawObjectiveBanner(bool isExtractionReady, int boxY)
    {
        string text = isExtractionReady
            ? "EXTRACTION READY — REACH THE GETAWAY VAN!"
            : $"OBJECTIVE: STEAL ${HeistConstants.TargetCashGoal:N0} FROM THE VAULT";

        Color background = isExtractionReady ? ReadyBgColor : ObjectiveBgColor;
        Color border = isExtractionReady ? ReadyBorderColor : ObjectiveBorderColor;
        Color textColor = isExtractionReady ? ReadyTextColor : ObjectiveTextColor;

        DrawCenteredBanner(text, boxY, background, border, textColor);
    }

    private static void DrawCenteredBanner(
        string text,
        int boxY,
        Color background,
        Color border,
        Color textColor)
    {
        int textWidth = UiTypography.MeasureTextWidth(text, BannerFontSize, bold: true);
        int boxWidth = textWidth + BannerHorizontalPadding;
        int boxX = (Raylib.GetScreenWidth() - boxWidth) / CenterDivisor;
        int textY = boxY + ((BannerHeight - BannerFontSize) / CenterDivisor);

        UiPanelDrawer.DrawTacticalCard(new Rectangle(boxX, boxY, boxWidth, BannerHeight), background, border, border);
        UiTypography.DrawText(text, boxX + (BannerHorizontalPadding / CenterDivisor), textY, BannerFontSize, textColor, bold: true);
    }

    private static void DrawWorldExtractionTag(Camera3D camera, RectangleBounds getawayZone)
    {
        Vector2 zoneCenter = getawayZone.Center;
        var worldPos = new Vector3(zoneCenter.X, MarkerWorldHeightY, zoneCenter.Y);
        if (HudPromptRenderer.TryProjectInFrontOfCamera(worldPos, camera, out Vector2 screenPos))
        {
            HudPromptRenderer.DrawPromptBox(ExtractMarkerText, screenPos);
        }
    }
}
