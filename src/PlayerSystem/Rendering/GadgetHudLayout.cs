using Raylib_cs;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Centralizes asset paths, layout metrics, and tactical colors for the bottom-left gadget HUD.
/// </summary>
public static class GadgetHudLayout
{
    public const string CoinIconPath = "assets/sprites/gadget_coin.png";
    public const string EmpIconPath = "assets/sprites/gadget_emp_grenade.png";
    public const string CoinDisplayName = "COIN";
    public const string EmpDisplayName = "EMP";
    public const string CoinFallbackBadge = "C";
    public const string EmpFallbackBadge = "E";

    public const int MarginLeft = 20;
    public const int MinTopMargin = 20;
    public const int BottomOffset = 94;
    public const int PanelHeight = 52;
    public const int IconBoxSize = PanelHeight;
    public const int IconPadding = 6;
    public const int BothSidesCount = 2;
    public const int CenterDivisor = 2;
    public const int IconDrawSize = IconBoxSize - (IconPadding * BothSidesCount);
    public const int SectionGap = 8;
    public const int InfoPaddingX = 12;
    public const int InfoPaddingY = 8;
    public const int HeaderFontSize = 15;
    public const int SlotsFontSize = 14;
    public const int RowSpacing = 7;
    public const int SlotsOffsetY = InfoPaddingY + HeaderFontSize + RowSpacing;
    public const int FallbackFontSize = 22;
    public const float TextureOrigin = 0f;
    public const float TextureRotation = 0f;

    public static readonly Color PanelBackground = new(15, 23, 42, 220);
    public static readonly Color CoinBorder = new(245, 197, 66, 220);
    public static readonly Color EmpReadyBorder = new(56, 189, 248, 220);
    public static readonly Color EmpPulseBorder = new(167, 139, 250, 240);
    public static readonly Color EmptyBorder = new(100, 116, 139, 190);
    public static readonly Color ActiveTextColor = new(224, 242, 254, 255);
    public static readonly Color SecondaryTextColor = new(148, 163, 184, 255);
    public static readonly Color EmpPulseTextColor = new(196, 181, 253, 255);
    public static readonly Color CoinAccentColor = new(250, 204, 21, 255);
    public static readonly Color EmpAccentColor = new(56, 225, 255, 255);
    public static readonly Color IconReadyTint = Color.White;
    public static readonly Color IconEmptyTint = new(148, 163, 184, 130);
}
