using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Interfaces;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.PlayerSystem.Gadgets;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders the equipped gadget sprite icon, active status, and slot summary in the bottom-left HUD.
/// </summary>
public sealed class GadgetHudRenderer
{
    private static readonly string[] IconAssetPaths =
    {
        GadgetHudLayout.CoinIconPath,
        GadgetHudLayout.EmpIconPath
    };

    private readonly Dictionary<string, Texture2D> _iconsByPath = new(StringComparer.OrdinalIgnoreCase);

    public void Load(ToonModelLibrary models)
    {
        ArgumentNullException.ThrowIfNull(models);
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        foreach (string path in IconAssetPaths)
        {
            if (!_iconsByPath.ContainsKey(path) && models.LoadTexture(path) is Texture2D texture)
            {
                _iconsByPath[path] = texture;
            }
        }
    }

    public void ClearCachedReferences() => _iconsByPath.Clear();

    public void Draw(PlayerGadgetLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        IGadget active = loadout.ActiveGadget;
        bool isEmpPulseActive = loadout.Emp.IsActivePulse;
        int panelY = Math.Max(GadgetHudLayout.MinTopMargin, Raylib.GetScreenHeight() - GadgetHudLayout.BottomOffset);
        Color borderColor = GadgetHudPresentation.ResolveBorderColor(active, isEmpPulseActive);

        DrawIconPanel(GadgetHudLayout.MarginLeft, panelY, active, borderColor);
        DrawInfoPanel(
            GadgetHudLayout.MarginLeft + GadgetHudLayout.IconBoxSize + GadgetHudLayout.SectionGap,
            panelY,
            loadout,
            borderColor);
    }

    private void DrawIconPanel(int panelX, int panelY, IGadget active, Color borderColor)
    {
        Raylib.DrawRectangle(panelX, panelY, GadgetHudLayout.IconBoxSize, GadgetHudLayout.IconBoxSize, GadgetHudLayout.PanelBackground);
        Raylib.DrawRectangleLines(panelX, panelY, GadgetHudLayout.IconBoxSize, GadgetHudLayout.IconBoxSize, borderColor);

        string iconPath = GadgetHudPresentation.ResolveIconPath(active);
        if (_iconsByPath.TryGetValue(iconPath, out Texture2D texture))
        {
            DrawIconTexture(texture, panelX, panelY, GadgetHudPresentation.ResolveIconTint(active));
            return;
        }

        DrawFallbackBadge(panelX, panelY, active);
    }

    private static void DrawIconTexture(Texture2D texture, int panelX, int panelY, Color tint)
    {
        var source = new Rectangle(GadgetHudLayout.TextureOrigin, GadgetHudLayout.TextureOrigin, texture.Width, texture.Height);
        var destination = new Rectangle(
            panelX + GadgetHudLayout.IconPadding,
            panelY + GadgetHudLayout.IconPadding,
            GadgetHudLayout.IconDrawSize,
            GadgetHudLayout.IconDrawSize);

        Raylib.DrawTexturePro(
            texture,
            source,
            destination,
            Vector2.Zero,
            GadgetHudLayout.TextureRotation,
            tint);
    }

    private static void DrawFallbackBadge(int panelX, int panelY, IGadget active)
    {
        string label = GadgetHudPresentation.ResolveFallbackBadge(active);
        int textWidth = Raylib.MeasureText(label, GadgetHudLayout.FallbackFontSize);
        int textX = panelX + CenterOffset(GadgetHudLayout.IconBoxSize, textWidth);
        int textY = panelY + CenterOffset(GadgetHudLayout.IconBoxSize, GadgetHudLayout.FallbackFontSize);

        Raylib.DrawText(label, textX, textY, GadgetHudLayout.FallbackFontSize, GadgetHudPresentation.ResolveFallbackColor(active));
    }

    private static int CenterOffset(int outerSize, int innerSize) =>
        (outerSize - innerSize) / GadgetHudLayout.CenterDivisor;

    private static void DrawInfoPanel(int panelX, int panelY, PlayerGadgetLoadout loadout, Color borderColor)
    {
        IGadget active = loadout.ActiveGadget;
        string headerText = GadgetHudPresentation.BuildHeaderText(active, loadout.Emp);
        string slotsText = GadgetHudPresentation.BuildSlotsText(loadout);

        int contentWidth = Math.Max(
            Raylib.MeasureText(headerText, GadgetHudLayout.HeaderFontSize),
            Raylib.MeasureText(slotsText, GadgetHudLayout.SlotsFontSize));
        int panelWidth = contentWidth + (GadgetHudLayout.InfoPaddingX * GadgetHudLayout.BothSidesCount);

        Raylib.DrawRectangle(panelX, panelY, panelWidth, GadgetHudLayout.PanelHeight, GadgetHudLayout.PanelBackground);
        Raylib.DrawRectangleLines(panelX, panelY, panelWidth, GadgetHudLayout.PanelHeight, borderColor);

        int textX = panelX + GadgetHudLayout.InfoPaddingX;
        Color headerColor = GadgetHudPresentation.ResolveHeaderTextColor(active, loadout.Emp.IsActivePulse);
        Raylib.DrawText(headerText, textX, panelY + GadgetHudLayout.InfoPaddingY, GadgetHudLayout.HeaderFontSize, headerColor);
        Raylib.DrawText(slotsText, textX, panelY + GadgetHudLayout.SlotsOffsetY, GadgetHudLayout.SlotsFontSize, GadgetHudLayout.SecondaryTextColor);
    }
}
