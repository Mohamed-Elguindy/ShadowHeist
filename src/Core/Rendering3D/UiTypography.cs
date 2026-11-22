using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Manages crisp TTF UI fonts with bilinear filtering and automatic fallback when running headlessly.
/// </summary>
public static class UiTypography
{
    public const string RegularFontPath = "assets/fonts/ui_regular.ttf";
    public const string BoldFontPath = "assets/fonts/ui_bold.ttf";

    private const int AtlasFontSize = 48;
    private const float LetterSpacing = 0.6f;
    private const int CenterDivisor = 2;
    private const int AsciiStart = 32;
    private const int AsciiEnd = 126;
    private const int EmDashCodepoint = 0x2014;

    private static readonly int[] SupportedCodepoints = BuildSupportedCodepoints();
    private static Font? _regularFont;
    private static Font? _boldFont;

    public static bool IsLoaded => _regularFont is not null && _boldFont is not null;

    public static void Load()
    {
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        _regularFont ??= LoadFontAsset(RegularFontPath);
        _boldFont ??= LoadFontAsset(BoldFontPath) ?? _regularFont;
    }

    public static void Unload()
    {
        if (!Raylib.IsWindowReady())
        {
            _regularFont = null;
            _boldFont = null;
            return;
        }

        if (_boldFont is Font bold && bold.Texture.Id != _regularFont?.Texture.Id)
        {
            Raylib.UnloadFont(bold);
        }

        if (_regularFont is Font regular)
        {
            Raylib.UnloadFont(regular);
        }

        _regularFont = null;
        _boldFont = null;
    }

    public static void DrawText(string text, int x, int y, int fontSize, Color color, bool bold = false)
    {
        if (string.IsNullOrEmpty(text) || !Raylib.IsWindowReady())
        {
            return;
        }

        if (SelectFont(bold) is Font font)
        {
            Raylib.DrawTextEx(font, text, new Vector2(x, y), fontSize, LetterSpacing, color);
            return;
        }

        Raylib.DrawText(text, x, y, fontSize, color);
    }

    public static int MeasureTextWidth(string text, int fontSize, bool bold = false)
    {
        if (string.IsNullOrEmpty(text) || !Raylib.IsWindowReady())
        {
            return 0;
        }

        if (SelectFont(bold) is Font font)
        {
            return (int)MathF.Ceiling(Raylib.MeasureTextEx(font, text, fontSize, LetterSpacing).X);
        }

        return Raylib.MeasureText(text, fontSize);
    }

    public static int CenterTextX(string text, int containerX, int containerWidth, int fontSize, bool bold = false) =>
        containerX + ((containerWidth - MeasureTextWidth(text, fontSize, bold)) / CenterDivisor);

    private static Font? SelectFont(bool bold) => bold ? _boldFont ?? _regularFont : _regularFont;

    private static Font? LoadFontAsset(string relativePath)
    {
        if (ResolveAssetPath(relativePath) is not string fullPath)
        {
            return null;
        }

        Font font = Raylib.LoadFontEx(fullPath, AtlasFontSize, SupportedCodepoints, SupportedCodepoints.Length);
        if (!Raylib.IsFontReady(font))
        {
            return null;
        }

        Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
        return font;
    }

    private static int[] BuildSupportedCodepoints()
    {
        int asciiCount = (AsciiEnd - AsciiStart) + 1;
        var codepoints = new int[asciiCount + 1];
        for (int i = 0; i < asciiCount; i++)
        {
            codepoints[i] = AsciiStart + i;
        }

        codepoints[asciiCount] = EmDashCodepoint;
        return codepoints;
    }

    private static string? ResolveAssetPath(string relativePath)
    {
        if (File.Exists(relativePath))
        {
            return relativePath;
        }

        string baseRelativePath = Path.Combine(AppContext.BaseDirectory, relativePath);
        return File.Exists(baseRelativePath) ? baseRelativePath : null;
    }
}
