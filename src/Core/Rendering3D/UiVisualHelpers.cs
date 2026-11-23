using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Shared asset resolution, bilinear texture loading, color alpha helpers, and easing curves.
/// </summary>
public static class UiVisualHelpers
{
    private const float HalfProgress = 0.5f;

    public static string? ResolveExistingAssetPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        if (File.Exists(relativePath))
        {
            return relativePath;
        }

        string baseRelativePath = Path.Combine(AppContext.BaseDirectory, relativePath);
        return File.Exists(baseRelativePath) ? baseRelativePath : null;
    }

    public static Texture2D? LoadBilinearTexture(string relativePath)
    {
        if (!Raylib.IsWindowReady() || ResolveExistingAssetPath(relativePath) is not string fullPath)
        {
            return null;
        }

        Texture2D texture = Raylib.LoadTexture(fullPath);
        if (texture.Id == 0)
        {
            return null;
        }

        Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
        return texture;
    }

    public static Color WithAlpha(Color rgb, byte alpha) =>
        new(rgb.R, rgb.G, rgb.B, alpha);

    public static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - (inv * inv * inv);
    }

    public static float EaseInCubic(float t) => t * t * t;

    public static float EaseInOutQuad(float t) =>
        t < HalfProgress ? 2f * t * t : 1f - (MathF.Pow((-2f * t) + 2f, 2f) * HalfProgress);
}
