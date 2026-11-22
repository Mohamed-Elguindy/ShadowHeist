namespace ShadowHeist.Core;

/// <summary>
/// High-level finite state machine coordinating MainMenu, LevelSelect, Playing, Paused, and Summary flows.
/// </summary>
public sealed class GameStateManager
{
    public static readonly int MainMenuOptionCount = Enum.GetValues<MainMenuOption>().Length;
    public static readonly int PauseMenuOptionCount = Enum.GetValues<PauseMenuOption>().Length;

    private readonly IReadOnlyList<LevelDescriptor> _levels;

    public GameFlowState CurrentState { get; private set; } = GameFlowState.MainMenu;
    public int SelectedLevelIndex { get; private set; }
    public int MenuOptionIndex { get; private set; }
    public MainMenuOption SelectedMainMenuOption => (MainMenuOption)MenuOptionIndex;
    public PauseMenuOption SelectedPauseMenuOption => (PauseMenuOption)MenuOptionIndex;
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

    public void OpenLevelSelect()
    {
        MenuOptionIndex = 0;
        CurrentState = GameFlowState.LevelSelect;
    }

    public void ReturnToMainMenu()
    {
        MenuOptionIndex = 0;
        CurrentState = GameFlowState.MainMenu;
    }

    public void ReturnToMainMenu(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.RestartMission();
        ReturnToMainMenu();
    }

    public void SelectNextMenuOption() =>
        MenuOptionIndex = WrapIndex(MenuOptionIndex, 1, ResolveMenuOptionCount());

    public void SelectPreviousMenuOption() =>
        MenuOptionIndex = WrapIndex(MenuOptionIndex, -1, ResolveMenuOptionCount());

    public void ActivateMainMenuOption(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        switch (SelectedMainMenuOption)
        {
            case MainMenuOption.StartMission:
                StartQuickPlay(context);
                break;
            case MainMenuOption.SelectLevel:
                OpenLevelSelect();
                break;
            case MainMenuOption.QuitGame:
                RequestExit();
                break;
        }
    }

    public void ActivatePauseMenuOption(GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        switch (SelectedPauseMenuOption)
        {
            case PauseMenuOption.Resume:
                ResumePlaying();
                break;
            case PauseMenuOption.ToggleOverviewCam:
                context.Camera.ToggleOverviewMode();
                break;
            case PauseMenuOption.RestartMission:
                RestartMission(context);
                break;
            case PauseMenuOption.MainMenu:
                ReturnToMainMenu(context);
                break;
        }
    }

    public void SelectNextLevel() =>
        SelectedLevelIndex = WrapIndex(SelectedLevelIndex, 1, _levels.Count);

    public void SelectPreviousLevel() =>
        SelectedLevelIndex = WrapIndex(SelectedLevelIndex, -1, _levels.Count);

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

    private int ResolveMenuOptionCount() =>
        CurrentState == GameFlowState.Paused ? PauseMenuOptionCount : MainMenuOptionCount;

    private static int WrapIndex(int currentIndex, int delta, int optionCount) =>
        (currentIndex + delta + optionCount) % optionCount;

    private void Pause()
    {
        MenuOptionIndex = 0;
        CurrentState = GameFlowState.Paused;
    }

    private void StartLevelAtIndex(int index, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        SelectLevel(index);
        context.SelectedMapPath = SelectedLevel.MapPath;
        context.RestartMission();
        CurrentState = GameFlowState.Playing;
    }
}
