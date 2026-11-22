using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Centralizes layout constants, tactical color palettes, and hit-test bounds for MenuOverlayRenderer and mouse menu input.
/// </summary>
public static class MenuLayoutConfig
{
    public const int PanelWidth = 620;
    public const int MainMenuPanelHeight = 424;
    public const int PauseMenuPanelHeight = 370;
    public const int LevelSelectBaseHeight = 200;
    public const int MenuHorizontalMargin = 48;
    public const int LevelListHorizontalMargin = 36;
    public const int MainLogoSize = 56;
    public const int MainLogoOffsetY = 24;
    public const int TitleFontSize = 36;
    public const int SubPanelTitleFontSize = 30;
    public const int PauseTitleFontSize = 32;
    public const int SubtitleFontSize = 15;
    public const int ItemFontSize = 19;
    public const int CardDetailFontSize = 14;
    public const int HintFontSize = 14;
    public const int MainTitleOffsetY = 92;
    public const int MainSubtitleOffsetY = 136;
    public const int MainOptionsStartOffsetY = 184;
    public const int PauseTitleOffsetY = 30;
    public const int PauseOptionsStartOffsetY = 90;
    public const int LevelSelectTitleOffsetY = 28;
    public const int LevelSelectListStartOffsetY = 84;
    public const int MenuRowHeight = 42;
    public const int MenuRowSpacing = 54;
    public const int MenuRowLabelOffsetY = 11;
    public const int LevelCardHeight = 68;
    public const int LevelCardSpacing = 82;
    public const int CardPaddingX = 16;
    public const int CardTitleOffsetY = 12;
    public const int CardStatusOffsetY = 14;
    public const int CardSubtitleOffsetY = 40;
    public const int FooterHintBottomMargin = 46;
    public const int LevelSelectFooterBottomMargin = 40;

    public static readonly Color BackdropColor = new(6, 10, 18, 178);
    public static readonly Color PanelBackground = new(12, 18, 32, 240);
    public static readonly Color PanelBorder = new(56, 189, 248, 220);
    public static readonly Color TitleAccent = new(56, 225, 255, 255);
    public static readonly Color SelectedRowBg = new(24, 48, 76, 245);
    public static readonly Color SelectedRowBorder = new(74, 222, 128, 240);
    public static readonly Color RowBg = new(18, 26, 44, 220);
    public static readonly Color RowBorder = new(70, 90, 124, 180);
    public static readonly Color MutedText = new(162, 178, 204, 255);
    public static readonly Color ReadyTagColor = new(74, 222, 128, 255);
    public static readonly Color FallbackTagColor = new(248, 196, 44, 255);

    public static Rectangle GetMainMenuOptionBounds(int screenWidth, int screenHeight, int optionIndex) =>
        GetOptionRowBounds(screenWidth, screenHeight, MainMenuPanelHeight, MainOptionsStartOffsetY, optionIndex);

    public static Rectangle GetPauseMenuOptionBounds(int screenWidth, int screenHeight, int optionIndex) =>
        GetOptionRowBounds(screenWidth, screenHeight, PauseMenuPanelHeight, PauseOptionsStartOffsetY, optionIndex);

    public static Rectangle GetLevelCardBounds(int screenWidth, int screenHeight, int levelCount, int levelIndex)
    {
        int panelHeight = LevelSelectBaseHeight + (levelCount * LevelCardSpacing);
        int panelX = (screenWidth - PanelWidth) / 2;
        int panelY = (screenHeight - panelHeight) / 2;
        int cardX = panelX + LevelListHorizontalMargin;
        int cardY = panelY + LevelSelectListStartOffsetY + (levelIndex * LevelCardSpacing);
        int cardWidth = PanelWidth - (LevelListHorizontalMargin * 2);
        return new Rectangle(cardX, cardY, cardWidth, LevelCardHeight);
    }

    public static int FindHoveredIndex(Vector2 pointerPosition, int itemCount, Func<int, Rectangle> boundsResolver)
    {
        for (int i = 0; i < itemCount; i++)
        {
            Rectangle bounds = boundsResolver(i);
            if (pointerPosition.X >= bounds.X &&
                pointerPosition.X <= bounds.X + bounds.Width &&
                pointerPosition.Y >= bounds.Y &&
                pointerPosition.Y <= bounds.Y + bounds.Height)
            {
                return i;
            }
        }

        return -1;
    }

    public static bool TryHandleMenuMouse(
        int optionCount,
        Func<int, Rectangle> boundsResolver,
        Action<int> selectAction,
        Action activateAction)
    {
        int hoveredIndex = FindHoveredIndex(Raylib.GetMousePosition(), optionCount, boundsResolver);
        if (hoveredIndex < 0)
        {
            return false;
        }

        bool clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);
        if (clicked || Raylib.GetMouseDelta() != Vector2.Zero)
        {
            selectAction(hoveredIndex);
        }

        if (clicked)
        {
            activateAction();
            return true;
        }

        return false;
    }

    private static Rectangle GetOptionRowBounds(
        int screenWidth,
        int screenHeight,
        int panelHeight,
        int optionsStartOffsetY,
        int optionIndex)
    {
        int panelX = (screenWidth - PanelWidth) / 2;
        int panelY = (screenHeight - panelHeight) / 2;
        int rowX = panelX + MenuHorizontalMargin;
        int rowY = panelY + optionsStartOffsetY + (optionIndex * MenuRowSpacing);
        int rowWidth = PanelWidth - (MenuHorizontalMargin * 2);
        return new Rectangle(rowX, rowY, rowWidth, MenuRowHeight);
    }
}
