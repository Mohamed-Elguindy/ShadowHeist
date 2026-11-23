using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders tactical screen-space overlays for MainMenu, LevelSelect, and Paused states.
/// </summary>
public static class MenuOverlayRenderer
{
    private const string NavigationHintText = "[UP/DOWN or W/S] NAVIGATE   |   [ENTER / CLICK] SELECT";
    private const string LevelSelectHintText = "[UP/DOWN] CHOOSE   |   [ENTER / CLICK] DEPLOY   |   [ESC] BACK";
    private const int HalfDivisor = 2;
    private const float TextureOrigin = 0f;
    private const float TextureRotation = 0f;

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
        DrawHeroLogo(panelX, panelY);

        DrawCenteredText("SHADOW HEIST", panelY + MenuLayoutConfig.MainTitleOffsetY, MenuLayoutConfig.TitleFontSize, MenuLayoutConfig.TitleAccent, bold: true);
        DrawCenteredText("TACTICAL STEALTH INFILTRATION", panelY + MenuLayoutConfig.MainSubtitleOffsetY, MenuLayoutConfig.SubtitleFontSize, MenuLayoutConfig.MutedText, bold: false);

        DrawOptionRows(panelX, panelY + MenuLayoutConfig.MainOptionsStartOffsetY, selectedOption, ResolveMainMenuLabel);
        DrawCenteredText(NavigationHintText, panelY + MenuLayoutConfig.MainMenuPanelHeight - MenuLayoutConfig.FooterHintBottomMargin, MenuLayoutConfig.HintFontSize, MenuLayoutConfig.MutedText, bold: false);
    }

    private static void DrawHeroLogo(int panelX, int panelY)
    {
        if (GameWindowChrome.BrandLogoTexture is not Texture2D logo)
        {
            return;
        }

        int logoX = panelX + ((MenuLayoutConfig.PanelWidth - MenuLayoutConfig.MainLogoSize) / HalfDivisor);
        int logoY = panelY + MenuLayoutConfig.MainLogoOffsetY;
        var source = new Rectangle(TextureOrigin, TextureOrigin, logo.Width, logo.Height);
        var destination = new Rectangle(logoX, logoY, MenuLayoutConfig.MainLogoSize, MenuLayoutConfig.MainLogoSize);
        Raylib.DrawTexturePro(logo, source, destination, Vector2.Zero, TextureRotation, Color.White);
    }

    private static void DrawLevelSelect(GameStateManager stateManager)
    {
        DrawBackdrop();
        int panelHeight = MenuLayoutConfig.LevelSelectBaseHeight + (stateManager.Levels.Count * MenuLayoutConfig.LevelCardSpacing);
        int panelX = CenterPanelX(MenuLayoutConfig.PanelWidth);
        int panelY = CenterPanelY(panelHeight);
        DrawPanelFrame(panelX, panelY, MenuLayoutConfig.PanelWidth, panelHeight);

        DrawCenteredText("SELECT CONTRACT", panelY + MenuLayoutConfig.LevelSelectTitleOffsetY, MenuLayoutConfig.SubPanelTitleFontSize, MenuLayoutConfig.TitleAccent, bold: true);
        DrawLevelEntries(
            stateManager,
            panelX + MenuLayoutConfig.LevelListHorizontalMargin,
            panelY + MenuLayoutConfig.LevelSelectListStartOffsetY,
            MenuLayoutConfig.PanelWidth - (MenuLayoutConfig.LevelListHorizontalMargin * HalfDivisor));
        DrawCenteredText(LevelSelectHintText, panelY + panelHeight - MenuLayoutConfig.LevelSelectFooterBottomMargin, MenuLayoutConfig.HintFontSize, MenuLayoutConfig.MutedText, bold: false);
    }

    private static void DrawPauseMenu(bool isOverviewCamera, PauseMenuOption selectedOption)
    {
        DrawBackdrop();
        int panelX = CenterPanelX(MenuLayoutConfig.PanelWidth);
        int panelY = CenterPanelY(MenuLayoutConfig.PauseMenuPanelHeight);
        DrawPanelFrame(panelX, panelY, MenuLayoutConfig.PanelWidth, MenuLayoutConfig.PauseMenuPanelHeight);

        DrawCenteredText("MISSION PAUSED", panelY + MenuLayoutConfig.PauseTitleOffsetY, MenuLayoutConfig.PauseTitleFontSize, MenuLayoutConfig.TitleAccent, bold: true);
        DrawOptionRows(panelX, panelY + MenuLayoutConfig.PauseOptionsStartOffsetY, selectedOption, option => ResolvePauseMenuLabel(option, isOverviewCamera));
        DrawCenteredText(NavigationHintText, panelY + MenuLayoutConfig.PauseMenuPanelHeight - MenuLayoutConfig.FooterHintBottomMargin, MenuLayoutConfig.HintFontSize, MenuLayoutConfig.MutedText, bold: false);
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
        int rowWidth = MenuLayoutConfig.PanelWidth - (MenuLayoutConfig.MenuHorizontalMargin * HalfDivisor);

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
        Color? accent = isSelected ? MenuLayoutConfig.SelectedRowBorder : null;
        UiPanelDrawer.DrawTacticalCard(new Rectangle(x, y, width, MenuLayoutConfig.LevelCardHeight), bg, border, accent);

        UiTypography.DrawText(level.Title, x + MenuLayoutConfig.CardPaddingX, y + MenuLayoutConfig.CardTitleOffsetY, MenuLayoutConfig.ItemFontSize, isSelected ? Color.White : MenuLayoutConfig.MutedText, bold: true);
        UiTypography.DrawText(level.Subtitle, x + MenuLayoutConfig.CardPaddingX, y + MenuLayoutConfig.CardSubtitleOffsetY, MenuLayoutConfig.CardDetailFontSize, MenuLayoutConfig.MutedText);

        string statusText = level.IsMapFilePresent ? "READY" : "FALLBACK";
        Color statusColor = level.IsMapFilePresent ? MenuLayoutConfig.ReadyTagColor : MenuLayoutConfig.FallbackTagColor;
        int statusWidth = UiTypography.MeasureTextWidth(statusText, MenuLayoutConfig.CardDetailFontSize, bold: true);
        UiTypography.DrawText(statusText, x + width - statusWidth - MenuLayoutConfig.CardPaddingX, y + MenuLayoutConfig.CardStatusOffsetY, MenuLayoutConfig.CardDetailFontSize, statusColor, bold: true);
    }

    private static void DrawBackdrop() =>
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), MenuLayoutConfig.BackdropColor);

    private static void DrawPanelFrame(int x, int y, int width, int height)
    {
        var bounds = new Rectangle(x, y, width, height);
        UiPanelDrawer.DrawTacticalCard(bounds, MenuLayoutConfig.PanelBackground, MenuLayoutConfig.PanelBorder);
        UiPanelDrawer.DrawBriefingCorners(bounds, MenuLayoutConfig.TitleAccent);
    }

    private static void DrawActionOption(int x, int y, int width, string label, bool isHighlighted)
    {
        Color bg = isHighlighted ? MenuLayoutConfig.SelectedRowBg : MenuLayoutConfig.RowBg;
        Color border = isHighlighted ? MenuLayoutConfig.SelectedRowBorder : MenuLayoutConfig.RowBorder;
        Color? accent = isHighlighted ? MenuLayoutConfig.SelectedRowBorder : null;
        UiPanelDrawer.DrawTacticalCard(new Rectangle(x, y, width, MenuLayoutConfig.MenuRowHeight), bg, border, accent);
        DrawCenteredTextInBox(label, x, y + MenuLayoutConfig.MenuRowLabelOffsetY, width, MenuLayoutConfig.ItemFontSize, Color.RayWhite, bold: isHighlighted);
    }

    private static void DrawCenteredText(string text, int y, int fontSize, Color color, bool bold) =>
        DrawCenteredTextInBox(text, 0, y, Raylib.GetScreenWidth(), fontSize, color, bold);

    private static void DrawCenteredTextInBox(string text, int boxX, int y, int boxWidth, int fontSize, Color color, bool bold)
    {
        int centeredX = UiTypography.CenterTextX(text, boxX, boxWidth, fontSize, bold);
        UiTypography.DrawText(text, centeredX, y, fontSize, color, bold);
    }

    private static int CenterPanelX(int width) => (Raylib.GetScreenWidth() - width) / HalfDivisor;

    private static int CenterPanelY(int height) => (Raylib.GetScreenHeight() - height) / HalfDivisor;
}
