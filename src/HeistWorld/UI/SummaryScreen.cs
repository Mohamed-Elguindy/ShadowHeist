using Raylib_cs;
using ShadowHeist.Core;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders the Mission Summary scoreboard overlay showing time, loot, alarms, and S/A/B/C stealth rank.
/// </summary>
public static class SummaryScreen
{
    private const int CardWidth = 640;
    private const int CardHeight = 356;
    private const int TitleFontSize = 30;
    private const int LabelFontSize = 20;
    private const int ValueFontSize = 22;
    private const int RankLetterFontSize = 68;
    private const int RankTitleFontSize = 18;
    private const int FooterFontSize = 18;
    private const int RowSpacing = 44;

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
        DrawSummaryCard(cardBounds, accentColor);
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

    private static void DrawSummaryCard(Rectangle cardBounds, Color accentColor)
    {
        Raylib.DrawRectangleRec(cardBounds, CardBackground);
        Raylib.DrawRectangleLinesEx(cardBounds, 2f, accentColor);
    }

    private static void DrawHeaderTitle(Rectangle cardBounds, HeistOutcome outcome, Color accentColor)
    {
        string title = outcome == HeistOutcome.Escaped
            ? "HEIST COMPLETE — ESCAPED!"
            : "BUSTED — CAUGHT BY SECURITY!";

        int x = CenterTextX(title, TitleFontSize, (int)cardBounds.X, (int)cardBounds.Width);
        Raylib.DrawText(title, x, (int)cardBounds.Y + 26, TitleFontSize, accentColor);
        Raylib.DrawLine(
            (int)cardBounds.X + 28,
            (int)cardBounds.Y + 72,
            (int)(cardBounds.X + cardBounds.Width) - 28,
            (int)cardBounds.Y + 72,
            MutedLabelColor);
    }

    private static void DrawStatRows(Rectangle cardBounds, MissionSummaryReport report)
    {
        int leftX = (int)cardBounds.X + 36;
        int valueX = (int)cardBounds.X + 210;
        int startY = (int)cardBounds.Y + 104;

        DrawStatRow("TIME", report.FormattedTime, leftX, valueX, startY, Color.RayWhite);
        DrawStatRow("LOOT", report.FormattedLoot, leftX, valueX, startY + RowSpacing, EscapedAccent);

        Color alarmColor = report.AlarmCount == 0 ? EscapedAccent : CaughtAccent;
        DrawStatRow("ALARMS", report.AlarmCount.ToString(), leftX, valueX, startY + (RowSpacing * 2), alarmColor);
    }

    private static void DrawStatRow(string label, string value, int labelX, int valueX, int y, Color valueColor)
    {
        Raylib.DrawText(label, labelX, y, LabelFontSize, MutedLabelColor);
        Raylib.DrawText(value, valueX, y - 1, ValueFontSize, valueColor);
    }

    private static void DrawRankBadge(Rectangle cardBounds, StealthRank rank, string rankTitle)
    {
        Color rankColor = ResolveRankColor(rank);
        int badgeX = (int)(cardBounds.X + cardBounds.Width) - 188;
        int badgeY = (int)cardBounds.Y + 96;
        const int badgeSize = 148;

        Raylib.DrawRectangle(badgeX, badgeY, badgeSize, badgeSize, RankBoxBackground);
        Raylib.DrawRectangleLines(badgeX, badgeY, badgeSize, badgeSize, rankColor);

        string letter = rank.ToString();
        int letterX = CenterTextX(letter, RankLetterFontSize, badgeX, badgeSize);
        Raylib.DrawText(letter, letterX, badgeY + 22, RankLetterFontSize, rankColor);

        int titleX = CenterTextX(rankTitle, RankTitleFontSize, badgeX - 18, badgeSize + 36);
        Raylib.DrawText(rankTitle, titleX, badgeY + badgeSize + 12, RankTitleFontSize, rankColor);
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
        const string footer = "Press [R] to Retry Mission";
        int x = CenterTextX(footer, FooterFontSize, (int)cardBounds.X, (int)cardBounds.Width);
        Raylib.DrawText(footer, x, (int)(cardBounds.Y + cardBounds.Height) - 42, FooterFontSize, Color.RayWhite);
    }

    private static int CenterTextX(string text, int fontSize, int containerX, int containerWidth)
    {
        int textWidth = Raylib.MeasureText(text, fontSize);
        return containerX + ((containerWidth - textWidth) / 2);
    }
}
