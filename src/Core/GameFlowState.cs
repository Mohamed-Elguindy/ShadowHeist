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
