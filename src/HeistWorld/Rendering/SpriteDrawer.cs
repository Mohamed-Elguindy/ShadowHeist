using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.HeistWorld.Rendering;

public sealed class SpriteDrawer : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = new();

    public void LoadTexture(string texturePath)
    {
        if (File.Exists(texturePath))
        {
            _textures[texturePath] = Raylib.LoadTexture(texturePath);
        }
    }

    public void DrawCentered(string texturePath, Vector2 center, float size, Color tint)
    {
        var destination = new Rectangle(center.X - size / 2f, center.Y - size / 2f, size, size);

        if (_textures.TryGetValue(texturePath, out Texture2D texture))
        {
            var source = new Rectangle(0, 0, texture.Width, texture.Height);
            Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, 0f, tint);
        }
        else
        {
            Raylib.DrawRectangleRec(destination, Color.Gold);
        }
    }

    public void Dispose()
    {
        foreach (Texture2D texture in _textures.Values)
        {
            Raylib.UnloadTexture(texture);
        }

        _textures.Clear();
    }
}
