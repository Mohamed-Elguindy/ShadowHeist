using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Centralizes dimensions, footer button bounds, and colors for the Mission Summary debrief overlay.
/// </summary>
public static class SummaryLayoutConfig
{
    public const int CardWidth = 760;
    public const int CardHeight = 468;
    public const int PaddingX = 28;
    public const int HeaderKickerY = 20;
    public const int HeaderTitleY = 40;
    public const int StatusPillWidth = 178;
    public const int StatusPillHeight = 28;
    public const int StatusPillY = 34;
    public const int HeaderDividerY = 82;
    public const int StatColumnWidth = 424;
    public const int StatStartY = 102;
    public const int StatCardHeight = 64;
    public const int StatCardGap = 12;
    public const int StatCardStep = StatCardHeight + StatCardGap;
    public const int StatInnerPadX = 16;
    public const int StatLabelY = 10;
    public const int StatValueY = 31;
    public const int StatTagY = 22;
    public const int ProgressBarHeight = 3;
    public const int ProgressBarBottomOffset = 6;
    public const int RankColumnX = 480;
    public const int RankSealCenterOffsetX = 126;
    public const int RankSealCenterY = 192;
    public const float RankSealOuterRadius = 74f;
    public const float RankSealInnerRadius = 62f;
    public const float RankRingInnerRadius = 78f;
    public const float RankRingOuterRadius = 82f;
    public const int RankHexagonSides = 6;
    public const float RankHexagonRotation = 30f;
    public const float RankBorderThickness = 2.5f;
    public const int RankTitlePillY = 284;
    public const int RankTitlePillWidth = 226;
    public const int RankTitlePillHeight = 30;
    public const int CommendationY = 334;
    public const int CommendationPillHeight = 24;
    public const int CommendationPillPadX = 12;
    public const int CommendationGap = 8;
    public const int TipY = 368;
    public const int FooterY = 402;
    public const int FooterButtonCount = 3;
    public const int FooterButtonHeight = 42;
    public const int FooterButtonGap = 14;
    public const int HalfDivisor = 2;

    public const int KickerFontSize = 13;
    public const int TitleFontSize = 26;
    public const int StatusPillFontSize = 12;
    public const int StatLabelFontSize = 12;
    public const int StatValueFontSize = 20;
    public const int StatTagFontSize = 13;
    public const int RankLetterFontSize = 58;
    public const int RankTitleFontSize = 14;
    public const int CommendationFontSize = 12;
    public const int TipFontSize = 13;
    public const int FooterButtonFontSize = 15;

    public static readonly Color BackdropRgb = new(6, 10, 18, 0);
    public static readonly Color CardBgRgb = new(10, 15, 28, 0);
    public static readonly Color SubCardBgRgb = new(15, 23, 42, 0);
    public static readonly Color SubCardBorderRgb = new(51, 65, 85, 0);
    public static readonly Color DividerRgb = new(51, 65, 85, 0);
    public static readonly Color EscapedAccent = new(74, 222, 128, 255);
    public static readonly Color CaughtAccent = new(244, 63, 94, 255);
    public static readonly Color MutedLabelColor = new(148, 163, 184, 255);
    public static readonly Color WarningColor = new(250, 204, 21, 255);
    public static readonly Color RankSColor = new(250, 204, 21, 255);
    public static readonly Color RankAColor = new(74, 222, 128, 255);
    public static readonly Color RankBColor = new(56, 189, 248, 255);
    public static readonly Color RankCColor = new(244, 63, 94, 255);
    public static readonly Color ButtonBgRgb = new(18, 26, 44, 0);
    public static readonly Color ButtonHoverBgRgb = new(28, 46, 74, 0);
    public static readonly Color ButtonBorderRgb = new(70, 90, 124, 0);

    public static Rectangle CreateCardBounds(int screenWidth, int screenHeight) =>
        new((screenWidth - CardWidth) / (float)HalfDivisor, (screenHeight - CardHeight) / (float)HalfDivisor, CardWidth, CardHeight);

    public static Rectangle GetFooterButtonBounds(int screenWidth, int screenHeight, int buttonIndex)
    {
        Rectangle card = CreateCardBounds(screenWidth, screenHeight);
        int innerWidth = CardWidth - (PaddingX * HalfDivisor);
        int totalGapWidth = FooterButtonGap * (FooterButtonCount - 1);
        int buttonWidth = (innerWidth - totalGapWidth) / FooterButtonCount;
        float x = card.X + PaddingX + (buttonIndex * (buttonWidth + FooterButtonGap));
        float y = card.Y + FooterY;
        return new Rectangle(x, y, buttonWidth, FooterButtonHeight);
    }

    public static bool TryHandleFooterMouse(GameContext context, int screenWidth, int screenHeight)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            return false;
        }

        int clicked = MenuLayoutConfig.FindHoveredIndex(
            Raylib.GetMousePosition(),
            FooterButtonCount,
            i => GetFooterButtonBounds(screenWidth, screenHeight, i));
        switch (clicked)
        {
            case 0:
                context.StateManager.RestartMission(context);
                return true;
            case 1:
                context.StateManager.OpenLevelSelect();
                return true;
            case 2:
                context.StateManager.ReturnToMainMenu(context);
                return true;
            default:
                return false;
        }
    }

    public static Color ResolveRankColor(StealthRank rank) => rank switch
    {
        StealthRank.S => RankSColor,
        StealthRank.A => RankAColor,
        StealthRank.B => RankBColor,
        _ => RankCColor
    };

    public static float EaseOutCubic(float t) => UiVisualHelpers.EaseOutCubic(t);

    public static Color WithAlpha(Color rgb, byte alpha) => UiVisualHelpers.WithAlpha(rgb, alpha);
}
