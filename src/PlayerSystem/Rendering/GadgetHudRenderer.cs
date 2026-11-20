using System.Globalization;
using Raylib_cs;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Gadgets;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders the active gadget slots, remaining charges, and EMP countdown in the bottom-left HUD.
/// </summary>
public static class GadgetHudRenderer
{
    private const int MarginLeft = 20;
    private const int BottomOffset = 74;
    private const int BoxHeight = 32;
    private const int HorizontalPadding = 10;
    private const int FontSize = 15;

    private static readonly Color PanelBackground = new(15, 23, 42, 220);
    private static readonly Color IdleBorder = new(56, 189, 248, 190);
    private static readonly Color EmpActiveBorder = new(167, 139, 250, 230);
    private static readonly Color ActiveTextColor = new(224, 242, 254, 255);
    private static readonly Color EmptyTextColor = new(148, 163, 184, 255);

    public static void Draw(PlayerGadgetLoadout loadout)
    {
        IGadget active = loadout.ActiveGadget;
        string empSuffix = loadout.Emp.IsActivePulse
            ? string.Create(CultureInfo.InvariantCulture, $" ({loadout.Emp.ActiveRemainingSeconds:0.0}s)")
            : string.Empty;

        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"[1] COIN x{loadout.Coin.RemainingCharges}   |   [2] EMP x{loadout.Emp.RemainingCharges}{empSuffix}   |   [Q] USE {active.Name.ToUpperInvariant()}");

        int textWidth = Raylib.MeasureText(text, FontSize);
        int boxWidth = textWidth + (HorizontalPadding * 2);
        int boxY = Raylib.GetScreenHeight() - BottomOffset;
        Color border = loadout.Emp.IsActivePulse ? EmpActiveBorder : IdleBorder;
        Color textColor = active.RemainingCharges > 0 ? ActiveTextColor : EmptyTextColor;

        Raylib.DrawRectangle(MarginLeft, boxY, boxWidth, BoxHeight, PanelBackground);
        Raylib.DrawRectangleLines(MarginLeft, boxY, boxWidth, BoxHeight, border);
        Raylib.DrawText(text, MarginLeft + HorizontalPadding, boxY + ((BoxHeight - FontSize) / 2), FontSize, textColor);
    }
}
