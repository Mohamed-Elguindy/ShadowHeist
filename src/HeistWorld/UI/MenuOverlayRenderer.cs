using Raylib_cs;
using ShadowHeist.Core;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Renders tactical screen-space overlays for MainMenu, LevelSelect, and Paused states.
/// </summary>
public static class MenuOverlayRenderer
{
    private const int PanelWidth = 620;
    private const int TitleFontSize = 38;
    private const int SubtitleFontSize = 18;
    private const int ItemFontSize = 20;
    private const int HintFontSize = 17;

    private static readonly Color BackdropColor = new(6, 10, 18, 178);
    private static readonly Color PanelBackground = new(12, 18, 32, 240);
    private static readonly Color PanelBorder = new(56, 189, 248, 220);
    private static readonly Color TitleAccent = new(56, 225, 255, 255);
    private static readonly Color SelectedRowBg = new(24, 48, 76, 245);
    private static readonly Color SelectedRowBorder = new(74, 222, 128, 240);
    private static readonly Color RowBg = new(18, 26, 44, 220);
    private static readonly Color RowBorder = new(70, 90, 124, 180);
    private static readonly Color MutedText = new(162, 178, 204, 255);
    private static readonly Color ReadyTagColor = new(74, 222, 128, 255);
    private static readonly Color FallbackTagColor = new(248, 196, 44, 255);

    public static void Draw(GameContext context)
    {
        switch (context.StateManager.CurrentState)
        {
            case GameFlowState.MainMenu:
                DrawMainMenu(context.StateManager.MenuOptionIndex);
                break;
            case GameFlowState.LevelSelect:
                DrawLevelSelect(context.StateManager);
                break;
            case GameFlowState.Paused:
                DrawPauseMenu(context.Camera.IsOverviewMode, context.StateManager.MenuOptionIndex);
                break;
        }
    }

    private static void DrawMainMenu(int selectedIndex)
    {
        DrawBackdrop();
        int panelX = CenterPanelX(PanelWidth);
        int panelY = CenterPanelY(360);
        DrawPanelFrame(panelX, panelY, PanelWidth, 360);

        DrawCenteredText("SHADOW HEIST", panelY + 34, TitleFontSize, TitleAccent);
        DrawCenteredText("3D TACTICAL STEALTH INFILTRATION", panelY + 84, SubtitleFontSize, MutedText);

        DrawActionOption(panelX + 48, panelY + 132, PanelWidth - 96, "START MISSION", selectedIndex == 0);
        DrawActionOption(panelX + 48, panelY + 186, PanelWidth - 96, "[L] SELECT LEVEL", selectedIndex == 1);
        DrawActionOption(panelX + 48, panelY + 240, PanelWidth - 96, "[Q / ESC] QUIT GAME", selectedIndex == 2);
        DrawCenteredText("[UP/DOWN or W/S] NAVIGATE   |   [ENTER] SELECT", panelY + 314, HintFontSize, MutedText);
    }

    private static void DrawLevelSelect(GameStateManager stateManager)
    {
        DrawBackdrop();
        int panelHeight = 200 + (stateManager.Levels.Count * 82);
        int panelX = CenterPanelX(PanelWidth);
        int panelY = CenterPanelY(panelHeight);
        DrawPanelFrame(panelX, panelY, PanelWidth, panelHeight);

        DrawCenteredText("SELECT CONTRACT", panelY + 28, 32, TitleAccent);
        DrawLevelEntries(stateManager, panelX + 36, panelY + 84, PanelWidth - 72);
        DrawCenteredText("[UP/DOWN] CHOOSE   |   [ENTER] DEPLOY   |   [ESC] BACK", panelY + panelHeight - 40, HintFontSize, MutedText);
    }

    private static void DrawLevelEntries(GameStateManager stateManager, int x, int startY, int width)
    {
        for (int i = 0; i < stateManager.Levels.Count; i++)
        {
            LevelDescriptor level = stateManager.Levels[i];
            bool isSelected = i == stateManager.SelectedLevelIndex;
            int rowY = startY + (i * 82);
            DrawLevelCard(level, x, rowY, width, isSelected);
        }
    }

    private static void DrawLevelCard(LevelDescriptor level, int x, int y, int width, bool isSelected)
    {
        Color bg = isSelected ? SelectedRowBg : RowBg;
        Color border = isSelected ? SelectedRowBorder : RowBorder;
        Raylib.DrawRectangle(x, y, width, 68, bg);
        Raylib.DrawRectangleLines(x, y, width, 68, border);

        Raylib.DrawText(level.Title, x + 16, y + 12, ItemFontSize, isSelected ? Color.White : MutedText);
        Raylib.DrawText(level.Subtitle, x + 16, y + 40, 15, MutedText);

        string statusText = level.IsMapFilePresent ? "READY" : "FALLBACK";
        Color statusColor = level.IsMapFilePresent ? ReadyTagColor : FallbackTagColor;
        int statusWidth = Raylib.MeasureText(statusText, 15);
        Raylib.DrawText(statusText, x + width - statusWidth - 16, y + 14, 15, statusColor);
    }

    private static void DrawPauseMenu(bool isOverviewCamera, int selectedIndex)
    {
        DrawBackdrop();
        int panelX = CenterPanelX(PanelWidth);
        int panelY = CenterPanelY(370);
        DrawPanelFrame(panelX, panelY, PanelWidth, 370);

        DrawCenteredText("MISSION PAUSED", panelY + 30, 34, TitleAccent);
        string cameraText = isOverviewCamera ? "[F6] SWITCH TO FOLLOW CAM" : "[F6] TACTICAL OVERVIEW";

        DrawActionOption(panelX + 48, panelY + 90, PanelWidth - 96, "[ESC] RESUME", selectedIndex == 0);
        DrawActionOption(panelX + 48, panelY + 144, PanelWidth - 96, cameraText, selectedIndex == 1);
        DrawActionOption(panelX + 48, panelY + 198, PanelWidth - 96, "[R] RESTART MISSION", selectedIndex == 2);
        DrawActionOption(panelX + 48, panelY + 252, PanelWidth - 96, "[M] MAIN MENU", selectedIndex == 3);
        DrawCenteredText("[UP/DOWN or W/S] NAVIGATE   |   [ENTER] SELECT", panelY + 324, HintFontSize, MutedText);
    }

    private static void DrawBackdrop() =>
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), BackdropColor);

    private static void DrawPanelFrame(int x, int y, int width, int height)
    {
        Raylib.DrawRectangle(x, y, width, height, PanelBackground);
        Raylib.DrawRectangleLines(x, y, width, height, PanelBorder);
    }

    private static void DrawActionOption(int x, int y, int width, string label, bool isHighlighted)
    {
        Color bg = isHighlighted ? SelectedRowBg : RowBg;
        Color border = isHighlighted ? SelectedRowBorder : RowBorder;
        Raylib.DrawRectangle(x, y, width, 42, bg);
        Raylib.DrawRectangleLines(x, y, width, 42, border);
        DrawCenteredTextInBox(label, x, y + 11, width, ItemFontSize, Color.RayWhite);
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
