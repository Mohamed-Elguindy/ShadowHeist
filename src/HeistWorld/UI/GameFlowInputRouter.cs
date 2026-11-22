using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Routes keyboard navigation and shortcuts per active GameFlowState to GameStateManager.
/// </summary>
public static class GameFlowInputRouter
{
    public static bool ProcessInput(GameContext context)
    {
        GameFlowState previousState = context.StateManager.CurrentState;
        HandleGlobalDisplayToggle();
        RouteByFlowState(context);
        return context.StateManager.CurrentState != previousState;
    }

    private static void HandleGlobalDisplayToggle()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F11))
        {
            GameWindowChrome.ToggleMaximizeRestore();
        }
    }

    private static void RouteByFlowState(GameContext context)
    {
        switch (context.StateManager.CurrentState)
        {
            case GameFlowState.MainMenu:
                HandleMainMenuInput(context);
                break;
            case GameFlowState.LevelSelect:
                HandleLevelSelectInput(context);
                break;
            case GameFlowState.Playing:
                HandlePlayingInput(context);
                break;
            case GameFlowState.Paused:
                HandlePausedInput(context);
                break;
            case GameFlowState.Summary:
                HandleSummaryInput(context);
                break;
        }
    }

    private static void HandleMainMenuInput(GameContext context)
    {
        if (MenuLayoutConfig.TryHandleMenuMouse(
            GameStateManager.MainMenuOptionCount,
            i => MenuLayoutConfig.GetMainMenuOptionBounds(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), i),
            context.StateManager.SelectMenuOption,
            () => context.StateManager.ActivateMainMenuOption(context)))
        {
            return;
        }

        if (IsMoveUpPressed())
        {
            context.StateManager.SelectPreviousMenuOption();
        }
        else if (IsMoveDownPressed())
        {
            context.StateManager.SelectNextMenuOption();
        }
        else if (IsConfirmPressed())
        {
            context.StateManager.ActivateMainMenuOption(context);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.L))
        {
            context.StateManager.OpenLevelSelect();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Q) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            context.StateManager.RequestExit();
        }
    }

    private static void HandleLevelSelectInput(GameContext context)
    {
        int levelCount = context.StateManager.Levels.Count;
        if (MenuLayoutConfig.TryHandleMenuMouse(
            levelCount,
            i => MenuLayoutConfig.GetLevelCardBounds(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), levelCount, i),
            context.StateManager.SelectLevel,
            () => context.StateManager.LaunchSelectedLevel(context)))
        {
            return;
        }

        if (IsMoveUpPressed())
        {
            context.StateManager.SelectPreviousLevel();
        }
        else if (IsMoveDownPressed())
        {
            context.StateManager.SelectNextLevel();
        }
        else if (IsConfirmPressed())
        {
            context.StateManager.LaunchSelectedLevel(context);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Backspace))
        {
            context.StateManager.ReturnToMainMenu();
        }
    }

    private static void HandlePlayingInput(GameContext context)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            context.StateManager.TogglePause();
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            context.StateManager.RestartMission(context);
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F6))
        {
            context.Camera.ToggleOverviewMode();
        }
    }

    private static void HandlePausedInput(GameContext context)
    {
        if (MenuLayoutConfig.TryHandleMenuMouse(
            GameStateManager.PauseMenuOptionCount,
            i => MenuLayoutConfig.GetPauseMenuOptionBounds(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), i),
            context.StateManager.SelectMenuOption,
            () => context.StateManager.ActivatePauseMenuOption(context)))
        {
            return;
        }

        if (IsMoveUpPressed())
        {
            context.StateManager.SelectPreviousMenuOption();
        }
        else if (IsMoveDownPressed())
        {
            context.StateManager.SelectNextMenuOption();
        }
        else if (IsConfirmPressed())
        {
            context.StateManager.ActivatePauseMenuOption(context);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            context.StateManager.ResumePlaying();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.F6))
        {
            context.Camera.ToggleOverviewMode();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            context.StateManager.RestartMission(context);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.M))
        {
            context.StateManager.ReturnToMainMenu(context);
        }
    }

    private static void HandleSummaryInput(GameContext context)
    {
        if (SummaryLayoutConfig.TryHandleFooterMouse(context, Raylib.GetScreenWidth(), Raylib.GetScreenHeight()))
        {
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            context.StateManager.RestartMission(context);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.L))
        {
            context.StateManager.OpenLevelSelect();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.M) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            context.StateManager.ReturnToMainMenu(context);
        }
    }

    private static bool IsMoveUpPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W);

    private static bool IsMoveDownPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S);

    private static bool IsConfirmPressed() =>
        Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Space);
}
