using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.HeistWorld;
using ShadowHeist.PlayerSystem;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class CutsceneControllerTests
{
    [Fact]
    public void DisabledCutscene_IsNoOpAndReadyForSummary()
    {
        var cutscene = new HeistCutsceneController { IsEnabled = false };
        var defaultVan = new Vector2(134f, 828f);

        cutscene.BeginEntry(new Vector2(128f, 640f));
        Assert.Equal(CutsceneMode.None, cutscene.Mode);
        Assert.True(cutscene.IsPlayerVisible());
        Assert.True(cutscene.IsReadyForSummary);
        Assert.Equal(defaultVan, cutscene.ResolveVanPosition(defaultVan));

        cutscene.BeginEscape(new Vector2(128f, 680f));
        Assert.Equal(CutsceneMode.None, cutscene.Mode);
        Assert.True(cutscene.IsReadyForSummary);
    }

    [Fact]
    public void EntryArrival_DrivesVanThenWalksPlayerToSpawnAndUnlocksControl()
    {
        var cutscene = new HeistCutsceneController { IsEnabled = true };
        var spawn = new Vector2(128f, 640f);
        var defaultVan = new Vector2(134f, 828f);

        cutscene.BeginEntry(spawn);
        Assert.True(cutscene.IsEntryPlaying);
        Assert.True(cutscene.IsBlockingPlayerControl);
        Assert.False(cutscene.IsPlayerVisible());
        Assert.True(cutscene.ResolveVanPosition(defaultVan).X < defaultVan.X);

        cutscene.Update(HeistCutsceneController.EntryDriveDurationSeconds + 0.1f);
        Assert.True(cutscene.IsPlayerVisible());
        Assert.Equal(defaultVan.X, cutscene.ResolveVanPosition(defaultVan).X, 0.1f);
        Vector2 midWalkPos = cutscene.ResolvePlayerPosition(spawn);
        Assert.True(midWalkPos.Y > spawn.Y && midWalkPos.Y < 774f);

        cutscene.Update(HeistCutsceneController.EntryWalkDurationSeconds);
        Assert.Equal(CutsceneMode.None, cutscene.Mode);
        Assert.False(cutscene.IsBlockingPlayerControl);
        Assert.Equal(spawn, cutscene.ResolvePlayerPosition(spawn));
    }

    [Fact]
    public void SkipEntry_ImmediatelyHandsControlToPlayer()
    {
        var cutscene = new HeistCutsceneController { IsEnabled = true };
        cutscene.BeginEntry(new Vector2(128f, 640f));

        cutscene.SkipEntry();

        Assert.Equal(CutsceneMode.None, cutscene.Mode);
        Assert.True(cutscene.WasEntrySkipped);
        Assert.False(cutscene.IsBlockingPlayerControl);
        Assert.True(cutscene.IsPlayerVisible());
    }

    [Fact]
    public void EscapeGetaway_BoardsVanDelaysSummaryThenDrivesAway()
    {
        var cutscene = new HeistCutsceneController { IsEnabled = true };
        var defaultVan = new Vector2(134f, 828f);

        cutscene.BeginEscape(new Vector2(128f, 670f));
        Assert.True(cutscene.IsEscapePlaying);
        Assert.True(cutscene.IsPlayerVisible());
        Assert.False(cutscene.IsReadyForSummary);

        cutscene.Update(HeistCutsceneController.EscapeBoardDurationSeconds + 0.25f);
        Assert.False(cutscene.IsPlayerVisible());
        Assert.False(cutscene.IsReadyForSummary);
        Assert.True(cutscene.ResolveVanPosition(defaultVan).X > defaultVan.X);

        cutscene.Update(HeistCutsceneController.EscapeDriveDurationSeconds);
        Assert.True(cutscene.IsReadyForSummary);
        Assert.True(cutscene.ResolveVanPosition(defaultVan).X > 700f);
    }

    [Fact]
    public void GameContextAndModules_FreezeMissionTimerAndTriggerEscapeCutscene()
    {
        using var context = new GameContext { Cutscene = { IsEnabled = true } };
        var heist = new HeistWorldModule();
        context.RegisterSystem(new PlayerSystemModule());
        context.RegisterSystem(heist);
        context.InitializeAll();

        context.StateManager.StartQuickPlay(context);
        Assert.True(context.Cutscene.IsEntryPlaying);

        context.UpdateAll(0.5f);
        Assert.Equal(0f, context.ElapsedSeconds);

        context.Cutscene.SkipEntry();
        context.UpdateAll(0.5f);
        Assert.Equal(0.5f, context.ElapsedSeconds, 0.01f);

        context.StolenCash = HeistConstants.TargetCashGoal;
        context.PlayerPosition = heist.GetawayZone.Center;
        heist.Update(0.016f, context);

        Assert.Equal(HeistOutcome.Escaped, context.Outcome);
        Assert.True(context.Cutscene.IsEscapePlaying);
    }
}
