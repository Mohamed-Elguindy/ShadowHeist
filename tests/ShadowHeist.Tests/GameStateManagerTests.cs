using System.Numerics;
using ShadowHeist.Core;
using ShadowHeist.HeistWorld;
using ShadowHeist.HeistWorld.UI;
using ShadowHeist.WorldLighting;
using Xunit;

namespace ShadowHeist.Tests;

public sealed class GameStateManagerTests
{
    [Fact]
    public void InitialState_StartsInMainMenuWithFirstLevelSelected()
    {
        var manager = new GameStateManager();

        Assert.Equal(GameFlowState.MainMenu, manager.CurrentState);
        Assert.Equal(0, manager.SelectedLevelIndex);
        Assert.Equal(HeistConstants.DefaultMapPath, manager.SelectedLevel.MapPath);
        Assert.False(manager.ShouldUpdateSimulation);
        Assert.False(manager.ExitRequested);
    }

    [Fact]
    public void OpenLevelSelectAndReturnToMainMenu_UpdatesCurrentState()
    {
        var manager = new GameStateManager();

        manager.OpenLevelSelect();
        Assert.Equal(GameFlowState.LevelSelect, manager.CurrentState);

        manager.ReturnToMainMenu();
        Assert.Equal(GameFlowState.MainMenu, manager.CurrentState);
    }

    [Fact]
    public void SelectNextAndPreviousLevel_CyclesThroughCatalogSafely()
    {
        var manager = new GameStateManager();
        int totalLevels = manager.Levels.Count;

        manager.SelectPreviousLevel();
        Assert.Equal(totalLevels - 1, manager.SelectedLevelIndex);

        manager.SelectNextLevel();
        Assert.Equal(0, manager.SelectedLevelIndex);

        manager.SelectLevel(999);
        Assert.Equal(totalLevels - 1, manager.SelectedLevelIndex);
    }

    [Fact]
    public void LaunchSelectedLevel_AppliesMapPathResetsMissionAndEntersPlaying()
    {
        using var context = new GameContext();
        context.StolenCash = 123_456;
        context.IsAlarmTriggered = true;

        context.StateManager.SelectNextLevel();
        context.StateManager.LaunchSelectedLevel(context);

        Assert.Equal(GameFlowState.Playing, context.StateManager.CurrentState);
        Assert.True(context.StateManager.ShouldUpdateSimulation);
        Assert.Equal(context.StateManager.SelectedLevel.MapPath, context.SelectedMapPath);
        Assert.Equal(0, context.StolenCash);
        Assert.False(context.IsAlarmTriggered);
    }

    [Fact]
    public void StartQuickPlay_SelectsFirstLevelAndStartsPlaying()
    {
        using var context = new GameContext();
        context.StateManager.SelectLevel(1);

        context.StateManager.StartQuickPlay(context);

        Assert.Equal(0, context.StateManager.SelectedLevelIndex);
        Assert.Equal(HeistConstants.DefaultMapPath, context.SelectedMapPath);
        Assert.Equal(GameFlowState.Playing, context.StateManager.CurrentState);
    }

    [Fact]
    public void TogglePauseAndResumePlaying_FreezesAndResumesSimulation()
    {
        using var context = new GameContext();
        context.StateManager.StartQuickPlay(context);

        context.StateManager.TogglePause();
        Assert.Equal(GameFlowState.Paused, context.StateManager.CurrentState);
        Assert.False(context.StateManager.ShouldUpdateSimulation);

        context.StateManager.TogglePause();
        Assert.Equal(GameFlowState.Playing, context.StateManager.CurrentState);
        Assert.True(context.StateManager.ShouldUpdateSimulation);

        context.StateManager.TogglePause();
        context.StateManager.ResumePlaying();
        Assert.Equal(GameFlowState.Playing, context.StateManager.CurrentState);
    }

    [Fact]
    public void TogglePause_OutsidePlayingOrPaused_DoesNothing()
    {
        using var context = new GameContext();

        context.StateManager.TogglePause();
        Assert.Equal(GameFlowState.MainMenu, context.StateManager.CurrentState);

        context.StateManager.StartQuickPlay(context);
        context.StateManager.SyncWithMissionOutcome(HeistOutcome.Caught);
        context.StateManager.TogglePause();
        Assert.Equal(GameFlowState.Summary, context.StateManager.CurrentState);
    }

    [Theory]
    [InlineData(HeistOutcome.Escaped)]
    [InlineData(HeistOutcome.Caught)]
    public void SyncWithMissionOutcome_WhenOutcomeResolves_TransitionsToSummary(HeistOutcome outcome)
    {
        using var context = new GameContext();
        context.StateManager.StartQuickPlay(context);

        context.StateManager.SyncWithMissionOutcome(outcome);

        Assert.Equal(GameFlowState.Summary, context.StateManager.CurrentState);
        Assert.False(context.StateManager.ShouldUpdateSimulation);
    }

    [Fact]
    public void RestartMission_FromSummaryOrPaused_ResetsMissionAndEntersPlaying()
    {
        using var context = new GameContext();
        context.StateManager.StartQuickPlay(context);
        context.Outcome = HeistOutcome.Caught;
        context.StateManager.SyncWithMissionOutcome(context.Outcome);

        context.StateManager.RestartMission(context);

        Assert.Equal(GameFlowState.Playing, context.StateManager.CurrentState);
        Assert.Equal(HeistOutcome.Playing, context.Outcome);
        Assert.Equal(0f, context.ElapsedSeconds);
    }

    [Fact]
    public void GameContext_UpdateAll_StopsAdvancingElapsedSecondsAfterOutcomeResolved()
    {
        using var context = new GameContext();
        context.UpdateAll(1.5f);
        Assert.Equal(1.5f, context.ElapsedSeconds, 0.001f);

        context.Outcome = HeistOutcome.Escaped;
        context.UpdateAll(2.0f);

        Assert.Equal(1.5f, context.ElapsedSeconds, 0.001f);
    }

    [Fact]
    public void HeistWorldModule_WhenCashGoalReached_MarksExtractionReadyAndEscapesInsideZone()
    {
        using var context = new GameContext();
        var world = new WorldLightingModule();
        var heist = new HeistWorldModule(world);
        context.RegisterSystem(world);
        context.RegisterSystem(heist);
        context.InitializeAll();

        Assert.False(heist.IsExtractionReady(context));

        context.StolenCash = HeistConstants.TargetCashGoal;
        Assert.True(heist.IsExtractionReady(context));

        context.PlayerPosition = heist.GetawayZone.Center;
        context.UpdateAll(0.016f);

        Assert.Equal(HeistOutcome.Escaped, context.Outcome);
    }

    [Fact]
    public void RequestExit_SetsExitRequestedFlag()
    {
        var manager = new GameStateManager();

        manager.RequestExit();

        Assert.True(manager.ExitRequested);
    }

    [Fact]
    public void MainMenuNavigation_WrapsOptionsAndActivatesSelectedAction()
    {
        using var context = new GameContext();
        GameStateManager manager = context.StateManager;

        manager.SelectNextMenuOption();
        Assert.Equal(MainMenuOption.SelectLevel, manager.SelectedMainMenuOption);

        manager.ActivateMainMenuOption(context);
        Assert.Equal(GameFlowState.LevelSelect, manager.CurrentState);

        manager.ReturnToMainMenu();
        manager.SelectPreviousMenuOption();
        Assert.Equal(MainMenuOption.QuitGame, manager.SelectedMainMenuOption);

        manager.ActivateMainMenuOption(context);
        Assert.True(manager.ExitRequested);
    }

    [Fact]
    public void PauseMenuNavigation_CyclesAllFourOptionsAndExecutesSelectedAction()
    {
        using var context = new GameContext();
        GameStateManager manager = context.StateManager;
        manager.StartQuickPlay(context);
        manager.TogglePause();

        Assert.Equal(PauseMenuOption.Resume, manager.SelectedPauseMenuOption);

        manager.SelectNextMenuOption();
        Assert.Equal(PauseMenuOption.ToggleOverviewCam, manager.SelectedPauseMenuOption);
        manager.ActivatePauseMenuOption(context);
        Assert.True(context.Camera.IsOverviewMode);

        manager.SelectNextMenuOption();
        Assert.Equal(PauseMenuOption.RestartMission, manager.SelectedPauseMenuOption);
        manager.ActivatePauseMenuOption(context);
        Assert.Equal(GameFlowState.Playing, manager.CurrentState);

        manager.TogglePause();
        manager.SelectPreviousMenuOption();
        Assert.Equal(PauseMenuOption.MainMenu, manager.SelectedPauseMenuOption);
        manager.ActivatePauseMenuOption(context);
        Assert.Equal(GameFlowState.MainMenu, manager.CurrentState);
    }

    [Fact]
    public void MenuMouseHitTesting_ResolvesMainMenuAndLevelSelectAndPauseRows()
    {
        using var context = new GameContext();
        GameStateManager manager = context.StateManager;

        var secondOptionBounds = MenuLayoutConfig.GetMainMenuOptionBounds(1280, 720, 1);
        var clickPoint = new Vector2(secondOptionBounds.X + 20f, secondOptionBounds.Y + 10f);
        int hoveredMain = MenuLayoutConfig.FindHoveredIndex(
            clickPoint,
            GameStateManager.MainMenuOptionCount,
            i => MenuLayoutConfig.GetMainMenuOptionBounds(1280, 720, i));

        Assert.Equal(1, hoveredMain);
        manager.SelectMenuOption(hoveredMain);
        manager.ActivateMainMenuOption(context);
        Assert.Equal(GameFlowState.LevelSelect, manager.CurrentState);

        var firstLevelBounds = MenuLayoutConfig.GetLevelCardBounds(1280, 720, manager.Levels.Count, 0);
        int hoveredLevel = MenuLayoutConfig.FindHoveredIndex(
            new Vector2(firstLevelBounds.X + 10f, firstLevelBounds.Y + 10f),
            manager.Levels.Count,
            i => MenuLayoutConfig.GetLevelCardBounds(1280, 720, manager.Levels.Count, i));

        Assert.Equal(0, hoveredLevel);
        manager.SelectLevel(hoveredLevel);
        manager.LaunchSelectedLevel(context);
        Assert.Equal(GameFlowState.Playing, manager.CurrentState);
    }
}
