using Raylib_cs;
using ShadowHeist.Core;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders tactical screen-space overlays for MainMenu, LevelSelect, and Paused states.
/// </summary>
public static class MenuOverlayRenderer
{
    private const string NavigationHintText = "[UP/DOWN or W/S] NAVIGATE   |   [ENTER / CLICK] SELECT";
    private const string LevelSelectHintText = "[UP/DOWN] CHOOSE   |   [ENTER / CLICK] DEPLOY   |   [ESC] BACK";

    public static void Draw(GameContext context)
    {
        switch (context.StateManager.CurrentState)
        {
            case GameFlowState.MainMenu:
                DrawMainMenu(context.StateManager.SelectedMainMenuOption);
                break;
            case GameFlowState.LevelSelect:
                DrawLevelSelect(context.StateManager);
                break;
            case GameFlowState.Paused:
                DrawPauseMenu(context.Camera.IsOverviewMode, context.StateManager.SelectedPauseMenuOption);
                break;
        }
    }

    private static void DrawMainMenu(MainMenuOption selectedOption)
    {
        DrawBackdrop();
        int panelX = CenterPanelX(MenuLayoutConfig.PanelWidth);
        int panelY = CenterPanelY(MenuLayoutConfig.MainMenuPanelHeight);
        DrawPanelFrame(panelX, panelY, MenuLayoutConfig.PanelWidth, MenuLayoutConfig.MainMenuPanelHeight);

        DrawCenteredText("SHADOW HEIST", panelY + MenuLayoutConfig.MainTitleOffsetY, MenuLayoutConfig.TitleFontSize, MenuLayoutConfig.TitleAccent);
        DrawCenteredText("3D TACTICAL STEALTH INFILTRATION", panelY + MenuLayoutConfig.MainSubtitleOffsetY, MenuLayoutConfig.SubtitleFontSize, MenuLayoutConfig.MutedText);

        DrawOptionRows(panelX, panelY + MenuLayoutConfig.MainOptionsStartOffsetY, selectedOption, ResolveMainMenuLabel);
        DrawCenteredText(NavigationHintText, panelY + MenuLayoutConfig.MainMenuPanelHeight - MenuLayoutConfig.FooterHintBottomMargin, MenuLayoutConfig.HintFontSize, MenuLayoutConfig.MutedText);
    }

    private static void DrawLevelSelect(GameStateManager stateManager)
    {
        DrawBackdrop();
        int panelHeight = MenuLayoutConfig.LevelSelectBaseHeight + (stateManager.Levels.Count * MenuLayoutConfig.LevelCardSpacing);
        int panelX = CenterPanelX(MenuLayoutConfig.PanelWidth);
        int panelY = CenterPanelY(panelHeight);
        DrawPanelFrame(panelX, panelY, MenuLayoutConfig.PanelWidth, panelHeight);

        DrawCenteredText("SELECT CONTRACT", panelY + MenuLayoutConfig.LevelSelectTitleOffsetY, MenuLayoutConfig.SubPanelTitleFontSize, MenuLayoutConfig.TitleAccent);
        DrawLevelEntries(
            stateManager,
            panelX + MenuLayoutConfig.LevelListHorizontalMargin,
            panelY + MenuLayoutConfig.LevelSelectListStartOffsetY,
            MenuLayoutConfig.PanelWidth - (MenuLayoutConfig.LevelListHorizontalMargin * 2));
        DrawCenteredText(LevelSelectHintText, panelY + panelHeight - MenuLayoutConfig.LevelSelectFooterBottomMargin, MenuLayoutConfig.HintFontSize, MenuLayoutConfig.MutedText);
    }

    private static void DrawPauseMenu(bool isOverviewCamera, PauseMenuOption selectedOption)
    {
        DrawBackdrop();
        int panelX = CenterPanelX(MenuLayoutConfig.PanelWidth);
        int panelY = CenterPanelY(MenuLayoutConfig.PauseMenuPanelHeight);
        DrawPanelFrame(panelX, panelY, MenuLayoutConfig.PanelWidth, MenuLayoutConfig.PauseMenuPanelHeight);

        DrawCenteredText("MISSION PAUSED", panelY + MenuLayoutConfig.PauseTitleOffsetY, MenuLayoutConfig.PauseTitleFontSize, MenuLayoutConfig.TitleAccent);
        DrawOptionRows(panelX, panelY + MenuLayoutConfig.PauseOptionsStartOffsetY, selectedOption, option => ResolvePauseMenuLabel(option, isOverviewCamera));
        DrawCenteredText(NavigationHintText, panelY + MenuLayoutConfig.PauseMenuPanelHeight - MenuLayoutConfig.FooterHintBottomMargin, MenuLayoutConfig.HintFontSize, MenuLayoutConfig.MutedText);
    }

    private static void DrawOptionRows<TOption>(
        int panelX,
        int startY,
        TOption selectedOption,
        Func<TOption, string> labelResolver)
        where TOption : struct, Enum
    {
        TOption[] options = Enum.GetValues<TOption>();
        int rowX = panelX + MenuLayoutConfig.MenuHorizontalMargin;
        int rowWidth = MenuLayoutConfig.PanelWidth - (MenuLayoutConfig.MenuHorizontalMargin * 2);

        for (int i = 0; i < options.Length; i++)
        {
            TOption option = options[i];
            int rowY = startY + (i * MenuLayoutConfig.MenuRowSpacing);
            DrawActionOption(rowX, rowY, rowWidth, labelResolver(option), EqualityComparer<TOption>.Default.Equals(option, selectedOption));
        }
    }

    private static string ResolveMainMenuLabel(MainMenuOption option) => option switch
    {
        MainMenuOption.StartMission => "START MISSION",
        MainMenuOption.SelectLevel => "[L] SELECT LEVEL",
        MainMenuOption.QuitGame => "[Q / ESC] QUIT GAME",
        _ => string.Empty
    };

    private static string ResolvePauseMenuLabel(PauseMenuOption option, bool isOverviewCamera) => option switch
    {
        PauseMenuOption.Resume => "[ESC] RESUME",
        PauseMenuOption.ToggleOverviewCam => isOverviewCamera ? "[F6] SWITCH TO FOLLOW CAM" : "[F6] TACTICAL OVERVIEW",
        PauseMenuOption.RestartMission => "[R] RESTART MISSION",
        PauseMenuOption.MainMenu => "[M] MAIN MENU",
        _ => string.Empty
    };

    private static void DrawLevelEntries(GameStateManager stateManager, int x, int startY, int width)
    {
        for (int i = 0; i < stateManager.Levels.Count; i++)
        {
            LevelDescriptor level = stateManager.Levels[i];
            bool isSelected = i == stateManager.SelectedLevelIndex;
            DrawLevelCard(level, x, startY + (i * MenuLayoutConfig.LevelCardSpacing), width, isSelected);
        }
    }

    private static void DrawLevelCard(LevelDescriptor level, int x, int y, int width, bool isSelected)
    {
        Color bg = isSelected ? MenuLayoutConfig.SelectedRowBg : MenuLayoutConfig.RowBg;
        Color border = isSelected ? MenuLayoutConfig.SelectedRowBorder : MenuLayoutConfig.RowBorder;
        Raylib.DrawRectangle(x, y, width, MenuLayoutConfig.LevelCardHeight, bg);
        Raylib.DrawRectangleLines(x, y, width, MenuLayoutConfig.LevelCardHeight, border);

        Raylib.DrawText(level.Title, x + MenuLayoutConfig.CardPaddingX, y + MenuLayoutConfig.CardTitleOffsetY, MenuLayoutConfig.ItemFontSize, isSelected ? Color.White : MenuLayoutConfig.MutedText);
        Raylib.DrawText(level.Subtitle, x + MenuLayoutConfig.CardPaddingX, y + MenuLayoutConfig.CardSubtitleOffsetY, MenuLayoutConfig.CardDetailFontSize, MenuLayoutConfig.MutedText);

        string statusText = level.IsMapFilePresent ? "READY" : "FALLBACK";
        Color statusColor = level.IsMapFilePresent ? MenuLayoutConfig.ReadyTagColor : MenuLayoutConfig.FallbackTagColor;
        int statusWidth = Raylib.MeasureText(statusText, MenuLayoutConfig.CardDetailFontSize);
        Raylib.DrawText(statusText, x + width - statusWidth - MenuLayoutConfig.CardPaddingX, y + MenuLayoutConfig.CardStatusOffsetY, MenuLayoutConfig.CardDetailFontSize, statusColor);
    }

    private static void DrawBackdrop() =>
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), MenuLayoutConfig.BackdropColor);

    private static void DrawPanelFrame(int x, int y, int width, int height)
    {
        Raylib.DrawRectangle(x, y, width, height, MenuLayoutConfig.PanelBackground);
        Raylib.DrawRectangleLines(x, y, width, height, MenuLayoutConfig.PanelBorder);
    }

    private static void DrawActionOption(int x, int y, int width, string label, bool isHighlighted)
    {
        Color bg = isHighlighted ? MenuLayoutConfig.SelectedRowBg : MenuLayoutConfig.RowBg;
        Color border = isHighlighted ? MenuLayoutConfig.SelectedRowBorder : MenuLayoutConfig.RowBorder;
        Raylib.DrawRectangle(x, y, width, MenuLayoutConfig.MenuRowHeight, bg);
        Raylib.DrawRectangleLines(x, y, width, MenuLayoutConfig.MenuRowHeight, border);
        DrawCenteredTextInBox(label, x, y + MenuLayoutConfig.MenuRowLabelOffsetY, width, MenuLayoutConfig.ItemFontSize, Color.RayWhite);
    }

    private static void DrawCenteredText(string text, int y, int fontSize, Color color) =>
        DrawCenteredTextInBox(text, 0, y, Raylib.GetScreenWidth(), fontSize, color);

    private static void DrawCenteredTextInBox(string text, int boxX, int y, int boxWidth, int fontSize, Color color)
    {
        int textWidth = Raylib.MeasureText(text, fontSize);
        Raylib.DrawText(text, boxX + ((boxWidth - textWidth) / 2), y, fontSize, color);
    }

    private static int CenterPanelX(int width) => (Raylib.GetScreenWidth() - width) / 2;

    private static int CenterPanelY(int height) => (Raylib.GetScreenHeight() - height) / 2;
}
