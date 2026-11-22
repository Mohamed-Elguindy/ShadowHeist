using System.Globalization;
using ShadowHeist.Core;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Immutable mission performance report displayed on the Summary screen.
/// </summary>
public readonly record struct MissionSummaryReport(
    HeistOutcome Outcome,
    StealthRank Rank,
    string RankTitle,
    string FormattedTime,
    string FormattedLoot,
    int AlarmCount);

/// <summary>
/// Extended mission debrief metrics used for par comparison, progress bars, commendations, and rank tips.
/// </summary>
public readonly record struct MissionDebriefDetails(
    MissionSummaryReport Report,
    string LevelTitle,
    float ElapsedSeconds,
    int StolenCash,
    int TargetCash,
    float LootRatio,
    bool IsUnderPar,
    IReadOnlyList<string> Commendations,
    string DebriefTip);

/// <summary>
/// Evaluates mission stealth ranks (S/A/B/C), debrief commendations, and formatted performance metrics.
/// </summary>
public static class MissionSummaryCalculator
{
    public const float FastClearSecondsThreshold = 90f;
    public const int ZeroAlarms = 0;
    public const int SingleAlarm = 1;
    private const int TenthsPerSecond = 10;
    private const int SecondsPerMinute = 60;
    private const int TenthsPerMinute = SecondsPerMinute * TenthsPerSecond;

    public static MissionSummaryReport BuildReport(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        int effectiveAlarms = ResolveEffectiveAlarmCount(context);
        StealthRank rank = EvaluateStealthRank(
            context.Outcome,
            context.StolenCash,
            effectiveAlarms,
            context.ElapsedSeconds);

        return new MissionSummaryReport(
            context.Outcome,
            rank,
            ResolveRankTitle(rank),
            FormatElapsedTime(context.ElapsedSeconds),
            FormatLootProgress(context.StolenCash, HeistConstants.TargetCashGoal),
            effectiveAlarms);
    }

    public static MissionDebriefDetails BuildDebrief(GameContext context)
    {
        MissionSummaryReport report = BuildReport(context);
        int targetCash = HeistConstants.TargetCashGoal;
        float lootRatio = targetCash > 0 ? Math.Clamp(context.StolenCash / (float)targetCash, 0f, 1f) : 0f;
        bool isUnderPar = context.ElapsedSeconds <= FastClearSecondsThreshold;
        IReadOnlyList<string> commendations = BuildCommendations(report.Outcome, context.StolenCash, targetCash, report.AlarmCount, isUnderPar);
        string tip = BuildDebriefTip(report.Outcome, report.Rank, context.StolenCash, targetCash, report.AlarmCount, isUnderPar);

        return new MissionDebriefDetails(
            report,
            context.StateManager.SelectedLevel.Title,
            context.ElapsedSeconds,
            context.StolenCash,
            targetCash,
            lootRatio,
            isUnderPar,
            commendations,
            tip);
    }

    public static StealthRank EvaluateStealthRank(
        HeistOutcome outcome,
        int stolenCash,
        int alarmCount,
        float elapsedSeconds)
    {
        if (outcome != HeistOutcome.Escaped || stolenCash < HeistConstants.TargetCashGoal)
        {
            return StealthRank.C;
        }

        if (alarmCount == ZeroAlarms && elapsedSeconds <= FastClearSecondsThreshold)
        {
            return StealthRank.S;
        }

        if (alarmCount == ZeroAlarms || (alarmCount == SingleAlarm && elapsedSeconds <= FastClearSecondsThreshold))
        {
            return StealthRank.A;
        }

        return StealthRank.B;
    }

    public static string ResolveRankTitle(StealthRank rank) => rank switch
    {
        StealthRank.S => "SHADOW MASTER",
        StealthRank.A => "SILENT PROFESSIONAL",
        StealthRank.B => "LOUD GETAWAY",
        _ => "COMPROMISED"
    };

    public static string FormatElapsedTime(float elapsedSeconds)
    {
        float clampedSeconds = MathF.Max(0f, elapsedSeconds);
        int totalTenths = (int)MathF.Round(clampedSeconds * TenthsPerSecond, MidpointRounding.AwayFromZero);
        int minutes = totalTenths / TenthsPerMinute;
        int remainingTenths = totalTenths % TenthsPerMinute;
        int wholeSeconds = remainingTenths / TenthsPerSecond;
        int tenths = remainingTenths % TenthsPerSecond;
        return string.Create(CultureInfo.InvariantCulture, $"{minutes:00}:{wholeSeconds:00}.{tenths}");
    }

    public static string FormatLootProgress(int stolenCash, int targetCash) =>
        string.Create(CultureInfo.InvariantCulture, $"${stolenCash:N0} / ${targetCash:N0}");

    private static IReadOnlyList<string> BuildCommendations(
        HeistOutcome outcome,
        int stolenCash,
        int targetCash,
        int alarmCount,
        bool isUnderPar)
    {
        var tags = new List<string>();
        if (alarmCount == ZeroAlarms)
        {
            tags.Add("ZERO ALARMS");
        }

        if (stolenCash >= targetCash)
        {
            tags.Add("FULL VAULT");
        }

        if (outcome == HeistOutcome.Escaped && isUnderPar)
        {
            tags.Add("UNDER PAR");
        }

        return tags;
    }

    private static string BuildDebriefTip(
        HeistOutcome outcome,
        StealthRank rank,
        int stolenCash,
        int targetCash,
        int alarmCount,
        bool isUnderPar)
    {
        if (rank == StealthRank.S)
        {
            return "FLAWLESS OPERATION — ZERO ALARMS AND UNDER 01:30.0 PAR";
        }

        if (outcome == HeistOutcome.Caught)
        {
            return "DEBRIEF: USE [C] SNEAK, WATCH VISION CONES, AND CUT POWER TO AVOID PURSUIT";
        }

        if (alarmCount > ZeroAlarms)
        {
            return "S-RANK TARGET: EXTRACT WITH 0 ALARMS UNDER 01:30.0 PAR";
        }

        if (stolenCash < targetCash)
        {
            return $"S-RANK TARGET: COLLECT ${targetCash:N0} FROM VAULT CRATES";
        }

        return isUnderPar
            ? "S-RANK TARGET: MAINTAIN FULL STEALTH TO ESCAPE"
            : "S-RANK TARGET: EXTRACT UNDER 01:30.0 WITH 0 ALARMS";
    }

    private static int ResolveEffectiveAlarmCount(GameContext context) =>
        context.IsAlarmTriggered ? Math.Max(1, context.AlarmCount) : context.AlarmCount;
}
