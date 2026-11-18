using Raylib_cs;

namespace ShadowHeist.HeistWorld.UI;

/// <summary>
/// Centralizes layout constants and tactical color palettes for MenuOverlayRenderer.
/// </summary>
internal static class MenuLayoutConfig
{
    public const int PanelWidth = 620;
    public const int MainMenuPanelHeight = 360;
    public const int PauseMenuPanelHeight = 370;
    public const int LevelSelectBaseHeight = 200;
    public const int MenuHorizontalMargin = 48;
    public const int LevelListHorizontalMargin = 36;
    public const int TitleFontSize = 38;
    public const int SubPanelTitleFontSize = 32;
    public const int PauseTitleFontSize = 34;
    public const int SubtitleFontSize = 18;
    public const int ItemFontSize = 20;
    public const int CardDetailFontSize = 15;
    public const int HintFontSize = 17;
    public const int MainTitleOffsetY = 34;
    public const int MainSubtitleOffsetY = 84;
    public const int MainOptionsStartOffsetY = 132;
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
}
