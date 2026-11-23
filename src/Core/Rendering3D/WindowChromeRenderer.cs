using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Renders the custom dark-navy header bar, ShadowHeist logo, mission tag, and vector window buttons.
/// </summary>
internal static class WindowChromeRenderer
{
    public static void Draw(
        Texture2D? logoTexture,
        GameStateManager stateManager,
        HeistOutcome outcome,
        WindowControlBounds controls)
    {
        int screenWidth = Raylib.GetScreenWidth();
        Raylib.DrawRectangle(0, 0, screenWidth, WindowChromeLayout.HeaderHeight, WindowChromeLayout.HeaderBackground);
        Raylib.DrawRectangle(
            0,
            WindowChromeLayout.HeaderHeight - WindowChromeLayout.BorderThickness,
            screenWidth,
            WindowChromeLayout.BorderThickness,
            WindowChromeLayout.HeaderBottomBorder);

        DrawLogoAndTitle(logoTexture, stateManager, outcome);
        DrawWindowControls(controls, Raylib.GetMousePosition());
    }

    private static void DrawLogoAndTitle(Texture2D? logoTexture, GameStateManager stateManager, HeistOutcome outcome)
    {
        DrawLogoIcon(logoTexture);

        int brandY = (WindowChromeLayout.HeaderHeight - WindowChromeLayout.BrandFontSize) / WindowChromeLayout.CenterDivisor;
        UiTypography.DrawText(
            WindowChromeLayout.BrandHeaderText,
            WindowChromeLayout.BrandOffsetX,
            brandY,
            WindowChromeLayout.BrandFontSize,
            WindowChromeLayout.BrandTitleColor,
            bold: true);

        int brandWidth = UiTypography.MeasureTextWidth(WindowChromeLayout.BrandHeaderText, WindowChromeLayout.BrandFontSize, bold: true);
        int separatorX = WindowChromeLayout.BrandOffsetX + brandWidth + WindowChromeLayout.TagSeparatorGap;
        int separatorY = (WindowChromeLayout.HeaderHeight - WindowChromeLayout.TagSeparatorHeight) / WindowChromeLayout.CenterDivisor;
        Raylib.DrawRectangle(
            separatorX,
            separatorY,
            WindowChromeLayout.TagSeparatorWidth,
            WindowChromeLayout.TagSeparatorHeight,
            WindowChromeLayout.SeparatorColor);

        string tag = WindowChromePresentation.ResolveHeaderTag(stateManager, outcome);
        int tagX = separatorX + WindowChromeLayout.TagSeparatorWidth + WindowChromeLayout.TagSeparatorGap;
        int tagY = (WindowChromeLayout.HeaderHeight - WindowChromeLayout.TagFontSize) / WindowChromeLayout.CenterDivisor;
        UiTypography.DrawText(tag, tagX, tagY, WindowChromeLayout.TagFontSize, WindowChromePresentation.ResolveTagColor(stateManager, outcome), bold: true);
    }

    private static void DrawLogoIcon(Texture2D? logoTexture)
    {
        if (logoTexture is Texture2D texture)
        {
            var source = new Rectangle(WindowChromeLayout.TextureOrigin, WindowChromeLayout.TextureOrigin, texture.Width, texture.Height);
            var destination = new Rectangle(
                WindowChromeLayout.LogoMarginLeft,
                WindowChromeLayout.LogoPaddingY,
                WindowChromeLayout.LogoSize,
                WindowChromeLayout.LogoSize);
            Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, WindowChromeLayout.TextureRotation, Color.White);
            return;
        }

        int centerX = WindowChromeLayout.LogoMarginLeft + (WindowChromeLayout.LogoSize / WindowChromeLayout.CenterDivisor);
        int centerY = WindowChromeLayout.HeaderHeight / WindowChromeLayout.CenterDivisor;
        Raylib.DrawCircleLines(centerX, centerY, WindowChromeLayout.FallbackBadgeRadius, WindowChromeLayout.FallbackCyan);
        Raylib.DrawCircle(centerX, centerY, WindowChromeLayout.GlyphHalfSpan, WindowChromeLayout.FallbackGold);
    }

    private static void DrawWindowControls(WindowControlBounds controls, Vector2 mouse)
    {
        bool minHover = Raylib.CheckCollisionPointRec(mouse, controls.MinimizeButton);
        bool maxHover = Raylib.CheckCollisionPointRec(mouse, controls.MaximizeButton);
        bool closeHover = Raylib.CheckCollisionPointRec(mouse, controls.CloseButton);

        DrawControlBackground(controls.MinimizeButton, minHover, isClose: false);
        DrawControlBackground(controls.MaximizeButton, maxHover, isClose: false);
        DrawControlBackground(controls.CloseButton, closeHover, isClose: true);

        DrawMinimizeGlyph(controls.MinimizeButton, ResolveIconColor(minHover));
        DrawMaximizeGlyph(controls.MaximizeButton, ResolveIconColor(maxHover));
        DrawCloseGlyph(controls.CloseButton, ResolveIconColor(closeHover));
    }

    private static void DrawControlBackground(Rectangle bounds, bool hovered, bool isClose)
    {
        if (!hovered)
        {
            return;
        }

        Color fill = isClose ? WindowChromeLayout.CloseHoverBackground : WindowChromeLayout.ButtonHoverBackground;
        Raylib.DrawRectangleRec(bounds, fill);
    }

    private static Color ResolveIconColor(bool hovered) =>
        hovered ? WindowChromeLayout.ButtonHoverIconColor : WindowChromeLayout.ButtonIconColor;

    private static void DrawMinimizeGlyph(Rectangle bounds, Color color)
    {
        int x = (int)(bounds.X + ((bounds.Width - WindowChromeLayout.MinimizeGlyphWidth) / WindowChromeLayout.CenterDivisor));
        int y = (int)(bounds.Y + ((bounds.Height - WindowChromeLayout.MinimizeGlyphHeight) / WindowChromeLayout.CenterDivisor));
        Raylib.DrawRectangle(x, y, WindowChromeLayout.MinimizeGlyphWidth, WindowChromeLayout.MinimizeGlyphHeight, color);
    }

    private static void DrawMaximizeGlyph(Rectangle bounds, Color color)
    {
        int x = (int)(bounds.X + ((bounds.Width - WindowChromeLayout.MaximizeGlyphSize) / WindowChromeLayout.CenterDivisor));
        int y = (int)(bounds.Y + ((bounds.Height - WindowChromeLayout.MaximizeGlyphSize) / WindowChromeLayout.CenterDivisor));

        if (!Raylib.IsWindowMaximized())
        {
            Raylib.DrawRectangleLinesEx(
                new Rectangle(x, y, WindowChromeLayout.MaximizeGlyphSize, WindowChromeLayout.MaximizeGlyphSize),
                WindowChromeLayout.GlyphLineThickness,
                color);
            return;
        }

        int frontSize = WindowChromeLayout.RestoreFrontSize;
        int offset = WindowChromeLayout.RestoreBackOffset;
        Raylib.DrawRectangleLinesEx(new Rectangle(x + offset, y, frontSize, frontSize), WindowChromeLayout.GlyphLineThickness, color);
        Raylib.DrawRectangle(x, y + offset, frontSize, frontSize, WindowChromeLayout.HeaderBackground);
        Raylib.DrawRectangleLinesEx(new Rectangle(x, y + offset, frontSize, frontSize), WindowChromeLayout.GlyphLineThickness, color);
    }

    private static void DrawCloseGlyph(Rectangle bounds, Color color)
    {
        float centerX = bounds.X + (bounds.Width / WindowChromeLayout.CenterDivisor);
        float centerY = bounds.Y + (bounds.Height / WindowChromeLayout.CenterDivisor);
        float half = WindowChromeLayout.GlyphHalfSpan;

        Raylib.DrawLineEx(new Vector2(centerX - half, centerY - half), new Vector2(centerX + half, centerY + half), WindowChromeLayout.GlyphLineThickness, color);
        Raylib.DrawLineEx(new Vector2(centerX - half, centerY + half), new Vector2(centerX + half, centerY - half), WindowChromeLayout.GlyphLineThickness, color);
    }
}
