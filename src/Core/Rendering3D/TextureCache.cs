using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Loads, caches by path, and safely unloads GPU Texture2D resources before window teardown.
/// </summary>
public sealed class TextureCache : IDisposable
{
    private readonly List<Texture2D> _textures = new();
    private readonly HashSet<uint> _ownedTextureIds = new();
    private readonly Dictionary<string, Texture2D> _texturesByPath = new(StringComparer.OrdinalIgnoreCase);

    public int CachedCount => _textures.Count;

    public Texture2D? Load(string texturePath, TextureFilter? filter = null)
    {
        if (!Raylib.IsWindowReady() || string.IsNullOrWhiteSpace(texturePath))
        {
            return null;
        }

        if (_texturesByPath.TryGetValue(texturePath, out Texture2D cached))
        {
            return cached;
        }

        if (ResolveExistingPath(texturePath) is not string resolvedPath)
        {
            return null;
        }

        Texture2D texture = Raylib.LoadTexture(resolvedPath);
        if (texture.Id == 0)
        {
            return null;
        }

        if (filter is TextureFilter appliedFilter)
        {
            Raylib.SetTextureFilter(texture, appliedFilter);
        }

        Track(texturePath, texture);
        return texture;
    }

    public bool Owns(uint textureId) => _ownedTextureIds.Contains(textureId);

    public void Dispose()
    {
        foreach (Texture2D texture in _textures)
        {
            Raylib.UnloadTexture(texture);
        }

        _textures.Clear();
        _ownedTextureIds.Clear();
        _texturesByPath.Clear();
    }

    private static string? ResolveExistingPath(string texturePath)
    {
        if (File.Exists(texturePath))
        {
            return texturePath;
        }

        string baseRelativePath = Path.Combine(AppContext.BaseDirectory, texturePath);
        return File.Exists(baseRelativePath) ? baseRelativePath : null;
    }

    private void Track(string texturePath, Texture2D texture)
    {
        _textures.Add(texture);
        _ownedTextureIds.Add(texture.Id);
        _texturesByPath[texturePath] = texture;
    }
}
