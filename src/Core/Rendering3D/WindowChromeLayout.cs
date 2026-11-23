using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Centralizes dimensions, asset paths, and dark tactical colors for GameWindowChrome.
/// </summary>
public static class WindowChromeLayout
{
    public const string BaseTitle = "ShadowHeist";
    public const string BrandHeaderText = "SHADOW HEIST";
    public const string WindowIconPath = "assets/sprites/app_icon.png";

    public const int DefaultWidth = 1280;
    public const int DefaultHeight = 720;
    public const int MinWidth = 960;
    public const int MinHeight = 540;
    public const int TargetFps = 60;

    public const int HeaderHeight = 36;
    public const int BorderThickness = 1;
    public const int CenterDivisor = 2;
    public const int LogoSize = 24;
    public const int LogoMarginLeft = 12;
    public const int LogoPaddingY = (HeaderHeight - LogoSize) / CenterDivisor;
    public const int BrandOffsetX = LogoMarginLeft + LogoSize + 10;
    public const int BrandFontSize = 15;
    public const int TagFontSize = 13;
    public const int TagSeparatorGap = 12;
    public const int TagSeparatorWidth = 1;
    public const int TagSeparatorHeight = 14;
    public const int ButtonWidth = 44;
    public const int ButtonCount = 3;
    public const int GlyphHalfSpan = 5;
    public const int MinimizeGlyphWidth = 10;
    public const int MinimizeGlyphHeight = 2;
    public const int MaximizeGlyphSize = 10;
    public const int RestoreBackOffset = 2;
    public const int RestoreFrontSize = 8;
    public const int FallbackBadgeRadius = 10;
    public const float GlyphLineThickness = 1.6f;
    public const float TextureOrigin = 0f;
    public const float TextureRotation = 0f;

    public static readonly Color HeaderBackground = new(8, 13, 26, 250);
    public static readonly Color HeaderBottomBorder = new(56, 189, 248, 95);
    public static readonly Color BrandTitleColor = new(224, 242, 254, 255);
    public static readonly Color SeparatorColor = new(51, 65, 85, 220);
    public static readonly Color NeutralTagColor = new(148, 163, 184, 255);
    public static readonly Color ActiveMissionTagColor = new(56, 189, 248, 255);
    public static readonly Color PausedTagColor = new(250, 204, 21, 255);
    public static readonly Color SuccessTagColor = new(74, 222, 128, 255);
    public static readonly Color AlertTagColor = new(244, 63, 94, 255);
    public static readonly Color ButtonHoverBackground = new(30, 41, 59, 240);
    public static readonly Color CloseHoverBackground = new(190, 18, 60, 240);
    public static readonly Color ButtonIconColor = new(203, 213, 225, 255);
    public static readonly Color ButtonHoverIconColor = Color.White;
    public static readonly Color FallbackGold = new(250, 204, 21, 255);
    public static readonly Color FallbackCyan = new(56, 189, 248, 255);
}
