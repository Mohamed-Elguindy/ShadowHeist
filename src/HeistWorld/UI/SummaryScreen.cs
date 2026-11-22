using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders the Mission Summary scoreboard overlay showing time, loot, alarms, and S/A/B/C stealth rank.
/// </summary>
public static class SummaryScreen
{
    private const int CardWidth = 640;
    private const int CardHeight = 356;
    private const int TitleFontSize = 28;
    private const int LabelFontSize = 18;
    private const int ValueFontSize = 20;
    private const int RankLetterFontSize = 62;
    private const int RankTitleFontSize = 16;
    private const int FooterFontSize = 15;
    private const int HeaderTitleOffsetY = 26;
    private const int DividerMarginX = 28;
    private const int DividerOffsetY = 72;
    private const int StatLabelOffsetX = 36;
    private const int StatValueOffsetX = 210;
    private const int StatStartOffsetY = 104;
    private const int StatValueVerticalAdjust = -1;
    private const int RowSpacing = 44;
    private const int RankBadgeSize = 148;
    private const int RankBadgeRightMargin = 188;
    private const int RankBadgeOffsetY = 96;
    private const int RankLetterOffsetY = 24;
    private const int RankTitleHorizontalPad = 18;
    private const int RankTitleOffsetY = 12;
    private const int FooterBottomMargin = 42;
    private const int HalfDivisor = 2;

    private static readonly Color BackdropColor = new(6, 10, 18, 190);
    private static readonly Color CardBackground = new(10, 15, 28, 244);
    private static readonly Color EscapedAccent = new(74, 222, 128, 255);
    private static readonly Color CaughtAccent = new(244, 63, 94, 255);
    private static readonly Color MutedLabelColor = new(162, 178, 204, 255);
    private static readonly Color RankBoxBackground = new(16, 24, 44, 240);
    private static readonly Color RankSColor = new(250, 204, 21, 255);
    private static readonly Color RankAColor = new(74, 222, 128, 255);
    private static readonly Color RankBColor = new(56, 189, 248, 255);
    private static readonly Color RankCColor = new(244, 63, 94, 255);

    public static void Draw(GameContext context)
    {
        if (context.Outcome == HeistOutcome.Playing)
        {
            return;
        }

        MissionSummaryReport report = MissionSummaryCalculator.BuildReport(context);
        Color accentColor = report.Outcome == HeistOutcome.Escaped ? EscapedAccent : CaughtAccent;
        Rectangle cardBounds = CreateCenteredCardBounds();

        DrawDimmedBackdrop();
        UiPanelDrawer.DrawTacticalCard(cardBounds, CardBackground, accentColor);
        UiPanelDrawer.DrawBriefingCorners(cardBounds, accentColor);
        DrawHeaderTitle(cardBounds, report.Outcome, accentColor);
        DrawStatRows(cardBounds, report);
        DrawRankBadge(cardBounds, report.Rank, report.RankTitle);
        DrawFooterPrompts(cardBounds);
    }

    private static Rectangle CreateCenteredCardBounds()
    {
        float x = (Raylib.GetScreenWidth() - CardWidth) * 0.5f;
        float y = (Raylib.GetScreenHeight() - CardHeight) * 0.5f;
        return new Rectangle(x, y, CardWidth, CardHeight);
    }

    private static void DrawDimmedBackdrop() =>
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), BackdropColor);

    private static void DrawHeaderTitle(Rectangle cardBounds, HeistOutcome outcome, Color accentColor)
    {
        string title = outcome == HeistOutcome.Escaped
            ? "HEIST COMPLETE — ESCAPED!"
            : "BUSTED — CAUGHT BY SECURITY!";

        int x = UiTypography.CenterTextX(title, (int)cardBounds.X, (int)cardBounds.Width, TitleFontSize, bold: true);
        UiTypography.DrawText(title, x, (int)cardBounds.Y + HeaderTitleOffsetY, TitleFontSize, accentColor, bold: true);
        Raylib.DrawLine(
            (int)cardBounds.X + DividerMarginX,
            (int)cardBounds.Y + DividerOffsetY,
            (int)(cardBounds.X + cardBounds.Width) - DividerMarginX,
            (int)cardBounds.Y + DividerOffsetY,
            MutedLabelColor);
    }

    private static void DrawStatRows(Rectangle cardBounds, MissionSummaryReport report)
    {
        int leftX = (int)cardBounds.X + StatLabelOffsetX;
        int valueX = (int)cardBounds.X + StatValueOffsetX;
        int startY = (int)cardBounds.Y + StatStartOffsetY;

        DrawStatRow("TIME", report.FormattedTime, leftX, valueX, startY, Color.RayWhite);
        DrawStatRow("LOOT", report.FormattedLoot, leftX, valueX, startY + RowSpacing, EscapedAccent);

        Color alarmColor = report.AlarmCount == 0 ? EscapedAccent : CaughtAccent;
        DrawStatRow("ALARMS", report.AlarmCount.ToString(), leftX, valueX, startY + (RowSpacing * HalfDivisor), alarmColor);
    }

    private static void DrawStatRow(string label, string value, int labelX, int valueX, int y, Color valueColor)
    {
        UiTypography.DrawText(label, labelX, y, LabelFontSize, MutedLabelColor, bold: true);
        UiTypography.DrawText(value, valueX, y + StatValueVerticalAdjust, ValueFontSize, valueColor, bold: true);
    }

    private static void DrawRankBadge(Rectangle cardBounds, StealthRank rank, string rankTitle)
    {
        Color rankColor = ResolveRankColor(rank);
        int badgeX = (int)(cardBounds.X + cardBounds.Width) - RankBadgeRightMargin;
        int badgeY = (int)cardBounds.Y + RankBadgeOffsetY;

        UiPanelDrawer.DrawTacticalCard(new Rectangle(badgeX, badgeY, RankBadgeSize, RankBadgeSize), RankBoxBackground, rankColor);

        string letter = rank.ToString();
        int letterX = UiTypography.CenterTextX(letter, badgeX, RankBadgeSize, RankLetterFontSize, bold: true);
        UiTypography.DrawText(letter, letterX, badgeY + RankLetterOffsetY, RankLetterFontSize, rankColor, bold: true);

        int titleX = UiTypography.CenterTextX(rankTitle, badgeX - RankTitleHorizontalPad, RankBadgeSize + (RankTitleHorizontalPad * HalfDivisor), RankTitleFontSize, bold: true);
        UiTypography.DrawText(rankTitle, titleX, badgeY + RankBadgeSize + RankTitleOffsetY, RankTitleFontSize, rankColor, bold: true);
    }

    private static Color ResolveRankColor(StealthRank rank) => rank switch
    {
        StealthRank.S => RankSColor,
        StealthRank.A => RankAColor,
        StealthRank.B => RankBColor,
        _ => RankCColor
    };

    private static void DrawFooterPrompts(Rectangle cardBounds)
    {
        const string footer = "[R] RETRY   |   [L] LEVEL SELECT   |   [M] MAIN MENU";
        int x = UiTypography.CenterTextX(footer, (int)cardBounds.X, (int)cardBounds.Width, FooterFontSize, bold: true);
        UiTypography.DrawText(footer, x, (int)(cardBounds.Y + cardBounds.Height) - FooterBottomMargin, FooterFontSize, Color.RayWhite, bold: true);
    }
}
