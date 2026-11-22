using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Computed reveal weights for the summary debrief fade-in and rank stamp animation.
/// </summary>
public readonly record struct SummaryRevealFrame(float CardAlpha, float SealAlpha, float SealScale);

/// <summary>
/// Renders the tactical After-Action Debrief overlay with stat sub-cards, rank seal, commendations, and action buttons.
/// </summary>
public static class SummaryScreen
{
    private const float RevealDurationSeconds = 0.45f;
    private const float CardFadeDurationSeconds = 0.25f;
    private const float SealPopStartSeconds = 0.12f;
    private const float SealScaleStart = 1.24f;
    private const float SealScaleRange = 0.24f;
    private const float FullCircleDegrees = 360f;
    private const int RingSegments = 36;
    private const float PercentScale = 100f;
    private const float MaxByte = 255f;
    private const float BackdropAlpha = 200f;
    private const float CardAlpha = 244f;
    private const float SubCardAlpha = 220f;
    private const float PanelBorderAlpha = 185f;
    private const float RingGlowAlpha = 130f;
    private const float ButtonAlpha = 235f;
    private const float MaxDeltaTime = 0.05f;

    private static readonly string[] FooterLabels = { "[R] RETRY", "[L] CONTRACTS", "[M] MAIN MENU" };
    private static float _revealSeconds;

    public static void Draw(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Outcome == HeistOutcome.Playing || !context.Cutscene.IsReadyForSummary)
        {
            _revealSeconds = 0f;
            return;
        }

        if (!Raylib.IsWindowReady())
        {
            return;
        }

        _revealSeconds = MathF.Min(RevealDurationSeconds, _revealSeconds + MathF.Min(Raylib.GetFrameTime(), MaxDeltaTime));
        SummaryRevealFrame reveal = EvaluateReveal(_revealSeconds);
        MissionDebriefDetails debrief = MissionSummaryCalculator.BuildDebrief(context);
        Color accent = debrief.Report.Outcome == HeistOutcome.Escaped ? SummaryLayoutConfig.EscapedAccent : SummaryLayoutConfig.CaughtAccent;
        Rectangle card = SummaryLayoutConfig.CreateCardBounds(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        DrawBackdropAndCard(card, accent, reveal.CardAlpha);
        DrawHeader(card, debrief, accent, reveal.CardAlpha);
        DrawStatColumn(card, debrief, reveal.CardAlpha);
        DrawRankSeal(card, debrief.Report.Rank, debrief.Report.RankTitle, reveal);
        DrawCommendationsAndTip(card, debrief, accent, reveal.CardAlpha);
        DrawFooterButtons(accent, reveal.CardAlpha);
    }

    public static SummaryRevealFrame EvaluateReveal(float elapsedSeconds)
    {
        float clamped = Math.Clamp(elapsedSeconds, 0f, RevealDurationSeconds);
        float cardAlpha = SummaryLayoutConfig.EaseOutCubic(Math.Clamp(clamped / CardFadeDurationSeconds, 0f, 1f));
        float sealT = Math.Clamp((clamped - SealPopStartSeconds) / (RevealDurationSeconds - SealPopStartSeconds), 0f, 1f);
        float sealEased = SummaryLayoutConfig.EaseOutCubic(sealT);
        return new SummaryRevealFrame(cardAlpha, sealEased, SealScaleStart - (SealScaleRange * sealEased));
    }

    private static void DrawBackdropAndCard(Rectangle card, Color accent, float alpha)
    {
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.BackdropRgb, (byte)(BackdropAlpha * alpha)));
        UiPanelDrawer.DrawTacticalCard(card, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.CardBgRgb, (byte)(CardAlpha * alpha)), SummaryLayoutConfig.WithAlpha(accent, (byte)(MaxByte * alpha)));
        UiPanelDrawer.DrawBriefingCorners(card, SummaryLayoutConfig.WithAlpha(accent, (byte)(MaxByte * alpha)));
    }

    private static void DrawHeader(Rectangle card, MissionDebriefDetails debrief, Color accent, float alpha)
    {
        int leftX = (int)card.X + SummaryLayoutConfig.PaddingX;
        string kicker = $"AFTER-ACTION DEBRIEF // {debrief.LevelTitle.ToUpperInvariant()}";
        UiTypography.DrawText(kicker, leftX, (int)card.Y + SummaryLayoutConfig.HeaderKickerY, SummaryLayoutConfig.KickerFontSize, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.MutedLabelColor, (byte)(MaxByte * alpha)), bold: true);

        string title = debrief.Report.Outcome == HeistOutcome.Escaped ? "EXTRACTION COMPLETE" : "OPERATION COMPROMISED";
        UiTypography.DrawText(title, leftX, (int)card.Y + SummaryLayoutConfig.HeaderTitleY, SummaryLayoutConfig.TitleFontSize, SummaryLayoutConfig.WithAlpha(accent, (byte)(MaxByte * alpha)), bold: true);

        int pillX = (int)(card.X + card.Width) - SummaryLayoutConfig.PaddingX - SummaryLayoutConfig.StatusPillWidth;
        var pillRect = new Rectangle(pillX, card.Y + SummaryLayoutConfig.StatusPillY, SummaryLayoutConfig.StatusPillWidth, SummaryLayoutConfig.StatusPillHeight);
        string statusText = debrief.Report.Outcome == HeistOutcome.Escaped ? "MISSION SUCCESS" : "SECURITY LOCKDOWN";
        UiPanelDrawer.DrawTacticalCard(pillRect, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.SubCardBgRgb, (byte)(ButtonAlpha * alpha)), SummaryLayoutConfig.WithAlpha(accent, (byte)(BackdropAlpha * alpha)));
        int textY = (int)pillRect.Y + ((SummaryLayoutConfig.StatusPillHeight - SummaryLayoutConfig.StatusPillFontSize) / SummaryLayoutConfig.HalfDivisor);
        UiTypography.DrawText(statusText, UiTypography.CenterTextX(statusText, pillX, SummaryLayoutConfig.StatusPillWidth, SummaryLayoutConfig.StatusPillFontSize, bold: true), textY, SummaryLayoutConfig.StatusPillFontSize, SummaryLayoutConfig.WithAlpha(accent, (byte)(MaxByte * alpha)), bold: true);

        int lineY = (int)card.Y + SummaryLayoutConfig.HeaderDividerY;
        Raylib.DrawLine(leftX, lineY, (int)(card.X + card.Width) - SummaryLayoutConfig.PaddingX, lineY, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.DividerRgb, (byte)(BackdropAlpha * alpha)));
    }

    private static void DrawStatColumn(Rectangle card, MissionDebriefDetails debrief, float alpha)
    {
        int x = (int)card.X + SummaryLayoutConfig.PaddingX;
        int startY = (int)card.Y + SummaryLayoutConfig.StatStartY;

        string timeTag = debrief.IsUnderPar ? "UNDER PAR (01:30)" : "OVER PAR (01:30)";
        Color timeColor = debrief.IsUnderPar ? SummaryLayoutConfig.EscapedAccent : SummaryLayoutConfig.WarningColor;
        DrawStatCard(x, startY, "CLEAR TIME", debrief.Report.FormattedTime, Color.RayWhite, timeTag, timeColor, alpha, progressRatio: null);

        string lootTag = $"{(int)MathF.Round(debrief.LootRatio * PercentScale)}% TARGET";
        Color lootColor = debrief.StolenCash >= debrief.TargetCash ? SummaryLayoutConfig.EscapedAccent : SummaryLayoutConfig.WarningColor;
        DrawStatCard(x, startY + SummaryLayoutConfig.StatCardStep, "STOLEN LOOT", debrief.Report.FormattedLoot, lootColor, lootTag, lootColor, alpha, debrief.LootRatio);

        string alarmTag = debrief.Report.AlarmCount == 0 ? "GHOST STEALTH" : $"DETECTED x{debrief.Report.AlarmCount}";
        Color alarmColor = debrief.Report.AlarmCount == 0 ? SummaryLayoutConfig.EscapedAccent : SummaryLayoutConfig.CaughtAccent;
        DrawStatCard(x, startY + (SummaryLayoutConfig.StatCardStep * SummaryLayoutConfig.HalfDivisor), "SECURITY ALARMS", debrief.Report.AlarmCount.ToString(), alarmColor, alarmTag, alarmColor, alpha, progressRatio: null);
    }

    private static void DrawStatCard(int x, int y, string label, string value, Color valueColor, string tag, Color tagColor, float alpha, float? progressRatio)
    {
        var bounds = new Rectangle(x, y, SummaryLayoutConfig.StatColumnWidth, SummaryLayoutConfig.StatCardHeight);
        UiPanelDrawer.DrawTacticalCard(bounds, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.SubCardBgRgb, (byte)(SubCardAlpha * alpha)), SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.SubCardBorderRgb, (byte)(PanelBorderAlpha * alpha)), SummaryLayoutConfig.WithAlpha(tagColor, (byte)(MaxByte * alpha)));

        int innerX = x + SummaryLayoutConfig.StatInnerPadX;
        UiTypography.DrawText(label, innerX, y + SummaryLayoutConfig.StatLabelY, SummaryLayoutConfig.StatLabelFontSize, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.MutedLabelColor, (byte)(MaxByte * alpha)), bold: true);
        UiTypography.DrawText(value, innerX, y + SummaryLayoutConfig.StatValueY, SummaryLayoutConfig.StatValueFontSize, SummaryLayoutConfig.WithAlpha(valueColor, (byte)(MaxByte * alpha)), bold: true);

        int tagWidth = UiTypography.MeasureTextWidth(tag, SummaryLayoutConfig.StatTagFontSize, bold: true);
        UiTypography.DrawText(tag, x + SummaryLayoutConfig.StatColumnWidth - tagWidth - SummaryLayoutConfig.StatInnerPadX, y + SummaryLayoutConfig.StatTagY, SummaryLayoutConfig.StatTagFontSize, SummaryLayoutConfig.WithAlpha(tagColor, (byte)(MaxByte * alpha)), bold: true);

        if (progressRatio is float ratio)
        {
            int trackWidth = SummaryLayoutConfig.StatColumnWidth - (SummaryLayoutConfig.StatInnerPadX * SummaryLayoutConfig.HalfDivisor);
            int trackY = y + SummaryLayoutConfig.StatCardHeight - SummaryLayoutConfig.ProgressBarBottomOffset;
            Raylib.DrawRectangle(innerX, trackY, trackWidth, SummaryLayoutConfig.ProgressBarHeight, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.SubCardBorderRgb, (byte)(PanelBorderAlpha * alpha)));
            Raylib.DrawRectangle(innerX, trackY, (int)(trackWidth * ratio), SummaryLayoutConfig.ProgressBarHeight, SummaryLayoutConfig.WithAlpha(tagColor, (byte)(MaxByte * alpha)));
        }
    }

    private static void DrawRankSeal(Rectangle card, StealthRank rank, string rankTitle, SummaryRevealFrame reveal)
    {
        Color rankColor = SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.ResolveRankColor(rank), (byte)(MaxByte * reveal.SealAlpha));
        var center = new Vector2(card.X + SummaryLayoutConfig.RankColumnX + SummaryLayoutConfig.RankSealCenterOffsetX, card.Y + SummaryLayoutConfig.RankSealCenterY);

        Raylib.DrawRing(center, SummaryLayoutConfig.RankRingInnerRadius * reveal.SealScale, SummaryLayoutConfig.RankRingOuterRadius * reveal.SealScale, 0f, FullCircleDegrees, RingSegments, SummaryLayoutConfig.WithAlpha(rankColor, (byte)(RingGlowAlpha * reveal.SealAlpha)));
        Raylib.DrawPoly(center, SummaryLayoutConfig.RankHexagonSides, SummaryLayoutConfig.RankSealOuterRadius * reveal.SealScale, SummaryLayoutConfig.RankHexagonRotation, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.SubCardBgRgb, (byte)(ButtonAlpha * reveal.SealAlpha)));
        Raylib.DrawPolyLinesEx(center, SummaryLayoutConfig.RankHexagonSides, SummaryLayoutConfig.RankSealInnerRadius * reveal.SealScale, SummaryLayoutConfig.RankHexagonRotation, SummaryLayoutConfig.RankBorderThickness, rankColor);

        string letter = rank.ToString();
        int letterX = (int)center.X - (UiTypography.MeasureTextWidth(letter, SummaryLayoutConfig.RankLetterFontSize, bold: true) / SummaryLayoutConfig.HalfDivisor);
        int letterY = (int)center.Y - (SummaryLayoutConfig.RankLetterFontSize / SummaryLayoutConfig.HalfDivisor);
        UiTypography.DrawText(letter, letterX, letterY, SummaryLayoutConfig.RankLetterFontSize, rankColor, bold: true);

        int pillX = (int)center.X - (SummaryLayoutConfig.RankTitlePillWidth / SummaryLayoutConfig.HalfDivisor);
        int pillY = (int)card.Y + SummaryLayoutConfig.RankTitlePillY;
        UiPanelDrawer.DrawTacticalCard(new Rectangle(pillX, pillY, SummaryLayoutConfig.RankTitlePillWidth, SummaryLayoutConfig.RankTitlePillHeight), SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.SubCardBgRgb, (byte)(ButtonAlpha * reveal.CardAlpha)), rankColor);
        int titleY = pillY + ((SummaryLayoutConfig.RankTitlePillHeight - SummaryLayoutConfig.RankTitleFontSize) / SummaryLayoutConfig.HalfDivisor);
        UiTypography.DrawText(rankTitle, UiTypography.CenterTextX(rankTitle, pillX, SummaryLayoutConfig.RankTitlePillWidth, SummaryLayoutConfig.RankTitleFontSize, bold: true), titleY, SummaryLayoutConfig.RankTitleFontSize, rankColor, bold: true);
    }

    private static void DrawCommendationsAndTip(Rectangle card, MissionDebriefDetails debrief, Color accent, float alpha)
    {
        int x = (int)card.X + SummaryLayoutConfig.PaddingX;
        int y = (int)card.Y + SummaryLayoutConfig.CommendationY;
        int textOffsetY = (SummaryLayoutConfig.CommendationPillHeight - SummaryLayoutConfig.CommendationFontSize) / SummaryLayoutConfig.HalfDivisor;

        foreach (string badge in debrief.Commendations)
        {
            int pillWidth = UiTypography.MeasureTextWidth(badge, SummaryLayoutConfig.CommendationFontSize, bold: true) + (SummaryLayoutConfig.CommendationPillPadX * SummaryLayoutConfig.HalfDivisor);
            UiPanelDrawer.DrawTacticalCard(new Rectangle(x, y, pillWidth, SummaryLayoutConfig.CommendationPillHeight), SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.SubCardBgRgb, (byte)(SubCardAlpha * alpha)), SummaryLayoutConfig.WithAlpha(accent, (byte)(PanelBorderAlpha * alpha)));
            UiTypography.DrawText(badge, x + SummaryLayoutConfig.CommendationPillPadX, y + textOffsetY, SummaryLayoutConfig.CommendationFontSize, SummaryLayoutConfig.WithAlpha(accent, (byte)(MaxByte * alpha)), bold: true);
            x += pillWidth + SummaryLayoutConfig.CommendationGap;
        }

        UiTypography.DrawText(debrief.DebriefTip, (int)card.X + SummaryLayoutConfig.PaddingX, (int)card.Y + SummaryLayoutConfig.TipY, SummaryLayoutConfig.TipFontSize, SummaryLayoutConfig.WithAlpha(SummaryLayoutConfig.MutedLabelColor, (byte)(MaxByte * alpha)));
    }

    private static void DrawFooterButtons(Color accent, float alpha)
    {
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        int hoveredIndex = MenuLayoutConfig.FindHoveredIndex(Raylib.GetMousePosition(), FooterLabels.Length, i => SummaryLayoutConfig.GetFooterButtonBounds(screenWidth, screenHeight, i));

        for (int i = 0; i < FooterLabels.Length; i++)
        {
            Rectangle bounds = SummaryLayoutConfig.GetFooterButtonBounds(screenWidth, screenHeight, i);
            bool isPrimary = i == 0;
            bool isHovered = i == hoveredIndex;
            Color bg = isHovered ? SummaryLayoutConfig.ButtonHoverBgRgb : SummaryLayoutConfig.ButtonBgRgb;
            Color border = isPrimary || isHovered ? accent : SummaryLayoutConfig.ButtonBorderRgb;
            Color textColor = isPrimary || isHovered ? Color.White : SummaryLayoutConfig.MutedLabelColor;

            UiPanelDrawer.DrawTacticalCard(bounds, SummaryLayoutConfig.WithAlpha(bg, (byte)(ButtonAlpha * alpha)), SummaryLayoutConfig.WithAlpha(border, (byte)(SubCardAlpha * alpha)), isPrimary ? SummaryLayoutConfig.WithAlpha(accent, (byte)(MaxByte * alpha)) : null);
            int textY = (int)bounds.Y + ((SummaryLayoutConfig.FooterButtonHeight - SummaryLayoutConfig.FooterButtonFontSize) / SummaryLayoutConfig.HalfDivisor);
            UiTypography.DrawText(FooterLabels[i], UiTypography.CenterTextX(FooterLabels[i], (int)bounds.X, (int)bounds.Width, SummaryLayoutConfig.FooterButtonFontSize, bold: true), textY, SummaryLayoutConfig.FooterButtonFontSize, SummaryLayoutConfig.WithAlpha(textColor, (byte)(MaxByte * alpha)), bold: true);
        }
    }
}
