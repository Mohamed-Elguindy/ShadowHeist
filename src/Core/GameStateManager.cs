namespace ShadowHeist.Core;

/// <summary>
/// High-level finite state machine coordinating MainMenu, LevelSelect, Playing, Paused, and Summary flows.
/// </summary>
public sealed class GameStateManager
{
    private readonly IReadOnlyList<LevelDescriptor> _levels;

    public GameFlowState CurrentState { get; private set; } = GameFlowState.MainMenu;
    public int SelectedLevelIndex { get; private set; }
    public bool ExitRequested { get; private set; }
    public LevelDescriptor SelectedLevel => _levels[SelectedLevelIndex];
    public IReadOnlyList<LevelDescriptor> Levels => _levels;
    public bool ShouldUpdateSimulation => CurrentState == GameFlowState.Playing;

    public GameStateManager(IReadOnlyList<LevelDescriptor>? levels = null)
    {
        _levels = levels is { Count: > 0 } ? levels : LevelCatalog.AllLevels;
    }

    public void StartQuickPlay(GameContext context) =>
        StartLevelAtIndex(0, context);

    public void OpenLevelSelect() =>
        CurrentState = GameFlowState.LevelSelect;

    public void ReturnToMainMenu() =>
        CurrentState = GameFlowState.MainMenu;

    public void ReturnToMainMenu(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.RestartMission();
        CurrentState = GameFlowState.MainMenu;
    }

    public void SelectNextLevel() =>
        SelectedLevelIndex = (SelectedLevelIndex + 1) % _levels.Count;

    public void SelectPreviousLevel() =>
        SelectedLevelIndex = (SelectedLevelIndex - 1 + _levels.Count) % _levels.Count;

    public void SelectLevel(int index) =>
        SelectedLevelIndex = Math.Clamp(index, 0, _levels.Count - 1);

    public void LaunchSelectedLevel(GameContext context) =>
        StartLevelAtIndex(SelectedLevelIndex, context);

    public void TogglePause()
    {
        if (CurrentState == GameFlowState.Playing)
        {
            Pause();
        }
        else if (CurrentState == GameFlowState.Paused)
        {
            ResumePlaying();
        }
    }

    public void ResumePlaying()
    {
        if (CurrentState == GameFlowState.Paused)
        {
            CurrentState = GameFlowState.Playing;
        }
    }

    public void RestartMission(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.RestartMission();
        CurrentState = GameFlowState.Playing;
    }

    public void SyncWithMissionOutcome(HeistOutcome outcome)
    {
        if (CurrentState == GameFlowState.Playing && outcome != HeistOutcome.Playing)
        {
            CurrentState = GameFlowState.Summary;
        }
    }

    public void RequestExit() =>
        ExitRequested = true;

    private void Pause() =>
        CurrentState = GameFlowState.Paused;

    private void StartLevelAtIndex(int index, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        SelectLevel(index);
        context.SelectedMapPath = SelectedLevel.MapPath;
        context.RestartMission();
        CurrentState = GameFlowState.Playing;
    }
}
