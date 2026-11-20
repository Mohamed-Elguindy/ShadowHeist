namespace ShadowHeist.Core;

/// <summary>
/// High-level application flow states governing menus, active gameplay, pause, and mission summary.
/// </summary>
public enum GameFlowState
{
    MainMenu,
    LevelSelect,
    Playing,
    Paused,
    Summary
}

/// <summary>
/// Ordered interactive options displayed on the Main Menu overlay.
/// </summary>
public enum MainMenuOption
{
    StartMission = 0,
    SelectLevel = 1,
    QuitGame = 2
}

/// <summary>
/// Ordered interactive options displayed on the Pause Menu overlay.
/// </summary>
public enum PauseMenuOption
{
    Resume = 0,
    ToggleOverviewCam = 1,
    RestartMission = 2,
    MainMenu = 3
}
