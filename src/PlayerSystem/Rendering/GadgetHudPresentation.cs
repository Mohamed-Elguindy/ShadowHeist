using System.Globalization;
using Raylib_cs;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.PlayerSystem.Gadgets;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Resolves icon assets, colors, and formatted labels for the bottom-left gadget HUD without GPU dependencies.
/// </summary>
public static class GadgetHudPresentation
{
    public static string ResolveIconPath(IGadget activeGadget)
    {
        ArgumentNullException.ThrowIfNull(activeGadget);
        return IsEmpGadget(activeGadget) ? GadgetHudLayout.EmpIconPath : GadgetHudLayout.CoinIconPath;
    }

    public static Color ResolveIconTint(IGadget activeGadget)
    {
        ArgumentNullException.ThrowIfNull(activeGadget);
        return activeGadget.RemainingCharges > 0 ? GadgetHudLayout.IconReadyTint : GadgetHudLayout.IconEmptyTint;
    }

    public static Color ResolveBorderColor(IGadget activeGadget, bool isEmpPulseActive)
    {
        ArgumentNullException.ThrowIfNull(activeGadget);
        if (isEmpPulseActive)
        {
            return GadgetHudLayout.EmpPulseBorder;
        }

        if (activeGadget.RemainingCharges <= 0)
        {
            return GadgetHudLayout.EmptyBorder;
        }

        return IsEmpGadget(activeGadget) ? GadgetHudLayout.EmpReadyBorder : GadgetHudLayout.CoinBorder;
    }

    public static Color ResolveHeaderTextColor(IGadget activeGadget, bool isEmpPulseActive)
    {
        ArgumentNullException.ThrowIfNull(activeGadget);
        if (isEmpPulseActive)
        {
            return GadgetHudLayout.EmpPulseTextColor;
        }

        return activeGadget.RemainingCharges > 0 ? GadgetHudLayout.ActiveTextColor : GadgetHudLayout.SecondaryTextColor;
    }

    public static string ResolveFallbackBadge(IGadget activeGadget)
    {
        ArgumentNullException.ThrowIfNull(activeGadget);
        return IsEmpGadget(activeGadget) ? GadgetHudLayout.EmpFallbackBadge : GadgetHudLayout.CoinFallbackBadge;
    }

    public static Color ResolveFallbackColor(IGadget activeGadget)
    {
        ArgumentNullException.ThrowIfNull(activeGadget);
        if (activeGadget.RemainingCharges <= 0)
        {
            return GadgetHudLayout.SecondaryTextColor;
        }

        return IsEmpGadget(activeGadget) ? GadgetHudLayout.EmpAccentColor : GadgetHudLayout.CoinAccentColor;
    }

    public static string BuildHeaderText(IGadget activeGadget, EmpGadget emp)
    {
        ArgumentNullException.ThrowIfNull(activeGadget);
        ArgumentNullException.ThrowIfNull(emp);

        string name = activeGadget.Name.ToUpperInvariant();
        if (IsEmpGadget(activeGadget) && emp.IsActivePulse)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"EQUIPPED: {name} x{activeGadget.RemainingCharges}   |   ACTIVE {emp.ActiveRemainingSeconds:0.0}s");
        }

        string status = activeGadget.RemainingCharges > 0 ? "[Q] USE" : "EMPTY";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"EQUIPPED: {name} x{activeGadget.RemainingCharges}   |   {status}");
    }

    public static string BuildSlotsText(PlayerGadgetLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        string empTimer = loadout.Emp.IsActivePulse
            ? string.Create(CultureInfo.InvariantCulture, $" ({loadout.Emp.ActiveRemainingSeconds:0.0}s)")
            : string.Empty;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"[1] {GadgetHudLayout.CoinDisplayName} x{loadout.Coin.RemainingCharges}   |   [2] {GadgetHudLayout.EmpDisplayName} x{loadout.Emp.RemainingCharges}{empTimer}   |   [TAB] SWAP");
    }

    private static bool IsEmpGadget(IGadget gadget) =>
        gadget is EmpGadget ||
        string.Equals(gadget.Name, GadgetHudLayout.EmpDisplayName, StringComparison.OrdinalIgnoreCase);
}
