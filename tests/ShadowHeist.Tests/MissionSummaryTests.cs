using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.HeistWorld.UI;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class MissionSummaryTests
{
    [Fact]
    public void EvaluateStealthRank_WhenEscapedUndetectedUnderThreshold_ReturnsRankS()
    {
        StealthRank rank = MissionSummaryCalculator.EvaluateStealthRank(
            HeistOutcome.Escaped,
            HeistConstants.TargetCashGoal,
            alarmCount: 0,
            elapsedSeconds: 74.2f);

        Assert.Equal(StealthRank.S, rank);
        Assert.Equal("SHADOW MASTER", MissionSummaryCalculator.ResolveRankTitle(rank));
    }

    [Fact]
    public void EvaluateStealthRank_WhenEscapedUndetectedOverThreshold_ReturnsRankA()
    {
        StealthRank rank = MissionSummaryCalculator.EvaluateStealthRank(
            HeistOutcome.Escaped,
            HeistConstants.TargetCashGoal,
            alarmCount: 0,
            elapsedSeconds: 125.0f);

        Assert.Equal(StealthRank.A, rank);
        Assert.Equal("SILENT PROFESSIONAL", MissionSummaryCalculator.ResolveRankTitle(rank));
    }

    [Fact]
    public void EvaluateStealthRank_WhenEscapedWithOneAlarmUnderThreshold_ReturnsRankA()
    {
        StealthRank rank = MissionSummaryCalculator.EvaluateStealthRank(
            HeistOutcome.Escaped,
            HeistConstants.TargetCashGoal,
            alarmCount: 1,
            elapsedSeconds: 85.0f);

        Assert.Equal(StealthRank.A, rank);
    }

    [Fact]
    public void EvaluateStealthRank_WhenEscapedWithMultipleAlarms_ReturnsRankB()
    {
        StealthRank rank = MissionSummaryCalculator.EvaluateStealthRank(
            HeistOutcome.Escaped,
            HeistConstants.TargetCashGoal,
            alarmCount: 2,
            elapsedSeconds: 60.0f);

        Assert.Equal(StealthRank.B, rank);
        Assert.Equal("LOUD GETAWAY", MissionSummaryCalculator.ResolveRankTitle(rank));
    }

    [Fact]
    public void EvaluateStealthRank_WhenCaughtWithFullLootAndZeroAlarms_ReturnsRankC()
    {
        StealthRank rank = MissionSummaryCalculator.EvaluateStealthRank(
            HeistOutcome.Caught,
            HeistConstants.TargetCashGoal,
            alarmCount: 0,
            elapsedSeconds: 20.0f);

        Assert.Equal(StealthRank.C, rank);
        Assert.Equal("COMPROMISED", MissionSummaryCalculator.ResolveRankTitle(rank));
    }

    [Theory]
    [InlineData(-5f, "00:00.0")]
    [InlineData(0f, "00:00.0")]
    [InlineData(59.96f, "01:00.0")]
    [InlineData(65.4f, "01:05.4")]
    [InlineData(119.97f, "02:00.0")]
    [InlineData(125.0f, "02:05.0")]
    public void FormatElapsedTime_FormatsMinutesSecondsAndTenths(float seconds, string expected)
    {
        Assert.Equal(expected, MissionSummaryCalculator.FormatElapsedTime(seconds));
    }

    [Fact]
    public void BuildReport_WhenAlarmTriggeredWithoutEvent_ReportsAtLeastOneAlarm()
    {
        using var context = new GameContext();
        context.StolenCash = HeistConstants.TargetCashGoal;
        context.IsAlarmTriggered = true;
        context.Outcome = HeistOutcome.Escaped;
        context.UpdateAll(45.0f);

        MissionSummaryReport report = MissionSummaryCalculator.BuildReport(context);

        Assert.Equal(1, report.AlarmCount);
        Assert.Equal(StealthRank.A, report.Rank);
        Assert.Equal("$500,000 / $500,000", report.FormattedLoot);
    }

    [Fact]
    public void GameContext_WhenAlarmEventsFire_CountsAlarmsFreezesTimerAndResetsOnRestart()
    {
        using var context = new GameContext();
        context.InitializeAll();
        context.UpdateAll(12.5f);

        context.EventBus.Publish(new AlarmTriggeredEvent(Vector2.Zero, "LaserTripwire:laser_hall"));
        context.EventBus.Publish(new AlarmTriggeredEvent(Vector2.Zero, "Guard:alpha"));
        context.StolenCash = HeistConstants.TargetCashGoal;
        context.Outcome = HeistOutcome.Escaped;
        context.UpdateAll(5.0f);

        MissionSummaryReport report = MissionSummaryCalculator.BuildReport(context);
        Assert.Equal(2, report.AlarmCount);
        Assert.Equal("00:12.5", report.FormattedTime);
        Assert.Equal(StealthRank.B, report.Rank);

        context.RestartMission();

        Assert.Equal(0, context.AlarmCount);
        Assert.Equal(0f, context.ElapsedSeconds);

        context.EventBus.Publish(new AlarmTriggeredEvent(Vector2.Zero, "Guard:beta"));
        Assert.Equal(1, context.AlarmCount);
    }

    [Fact]
    public void BuildDebrief_ComputesCommendationsParLootRatioAndTip()
    {
        using var context = new GameContext();
        context.InitializeAll();
        context.UpdateAll(60.0f);
        context.StolenCash = HeistConstants.TargetCashGoal;
        context.Outcome = HeistOutcome.Escaped;

        MissionDebriefDetails debrief = MissionSummaryCalculator.BuildDebrief(context);

        Assert.Equal(StealthRank.S, debrief.Report.Rank);
        Assert.Equal(1f, debrief.LootRatio, 0.01f);
        Assert.True(debrief.IsUnderPar);
        Assert.Contains("ZERO ALARMS", debrief.Commendations);
        Assert.Contains("FULL VAULT", debrief.Commendations);
        Assert.Contains("UNDER PAR", debrief.Commendations);
        Assert.Contains("FLAWLESS", debrief.DebriefTip);
    }

    [Fact]
    public void SummaryScreenRevealAndFooterButtons_ComputeCleanlyHeadlessly()
    {
        SummaryRevealFrame start = SummaryScreen.EvaluateReveal(0f);
        SummaryRevealFrame end = SummaryScreen.EvaluateReveal(1f);

        Assert.Equal(0f, start.CardAlpha, 0.01f);
        Assert.True(start.SealScale > 1f);
        Assert.Equal(1f, end.CardAlpha, 0.01f);
        Assert.Equal(1f, end.SealAlpha, 0.01f);
        Assert.Equal(1f, end.SealScale, 0.01f);

        var retryBounds = SummaryLayoutConfig.GetFooterButtonBounds(1280, 720, 0);
        var contractsBounds = SummaryLayoutConfig.GetFooterButtonBounds(1280, 720, 1);
        var menuBounds = SummaryLayoutConfig.GetFooterButtonBounds(1280, 720, 2);

        Assert.True(retryBounds.X < contractsBounds.X && contractsBounds.X < menuBounds.X);
        Assert.Equal(retryBounds.Width, menuBounds.Width, 0.01f);

        using var context = new GameContext { Outcome = HeistOutcome.Caught };
        SummaryScreen.Draw(context);
    }
}
