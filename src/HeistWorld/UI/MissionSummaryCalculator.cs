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
/// Evaluates mission stealth ranks (S/A/B/C) and formats timer, loot, and alarm metrics.
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

    private static int ResolveEffectiveAlarmCount(GameContext context) =>
        context.IsAlarmTriggered ? Math.Max(1, context.AlarmCount) : context.AlarmCount;
}
