using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders the Player sprite and visual orientation (Ticket #4 - Mahmoud).
/// Decoupled from Player movement logic (Single Responsibility Principle).
/// </summary>
public sealed class PlayerRenderer : IDisposable
{
    private const string SpritePath = "assets/sprites/player_sneak.png";
    private const float SpriteSize = 48f;
    private const float SpriteFacesUpOffsetDegrees = 90f;
    private static readonly Vector2 SpriteOrigin = new(SpriteSize / 2f, SpriteSize / 2f);
    private readonly Texture2D _texture;
    private readonly bool _hasTexture;

    public PlayerRenderer()
    {
        _texture = Raylib.LoadTexture(SpritePath);
        _hasTexture = _texture.Id > 0;
    }

    public void Draw(Player player)
    {
        if (_hasTexture)
        {
            var source = new Rectangle(0, 0, _texture.Width, _texture.Height);
            var dest = new Rectangle(player.Position.X, player.Position.Y, SpriteSize, SpriteSize);
            float spriteRotation = player.RotationDegrees + SpriteFacesUpOffsetDegrees;
            Raylib.DrawTexturePro(_texture, source, dest, SpriteOrigin, spriteRotation, Color.White);
        }
        else
        {
            // Fallback rendering if the sprite fails to load
            Raylib.DrawCircleV(player.Position, player.Radius, Color.SkyBlue);

            // Draw a line showing facing direction
            float radians = player.RotationDegrees * (MathF.PI / 180f);
            var facingEnd = player.Position + new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * 24f;
            Raylib.DrawLineV(player.Position, facingEnd, Color.White);
        }
    }

    public void Dispose()
    {
        if (_hasTexture)
        {
            Raylib.UnloadTexture(_texture);
        }
    }
}

