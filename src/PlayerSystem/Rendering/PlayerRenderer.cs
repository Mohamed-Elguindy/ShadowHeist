using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders the Player sprite and visual orientation.
/// </summary>
public sealed class PlayerRenderer : IDisposable
{
    private const string SpritePath = "assets/sprites/player_sneak.png";
    private const float SpriteSize = 48f;
    private const float SpriteFacesUpOffsetDegrees = 90f;
    private const float FallbackAimLineLength = 24f;
    private static readonly Vector2 SpriteOrigin = new(SpriteSize / 2f, SpriteSize / 2f);

    private readonly Texture2D _texture;
    private readonly bool _hasTexture;

    public PlayerRenderer()
    {
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        _texture = Raylib.LoadTexture(SpritePath);
        _hasTexture = _texture.Id > 0;
    }

    public void Draw(Player player)
    {
        if (_hasTexture)
        {
            DrawSprite(player);
        }
        else
        {
            DrawFallbackCircleWithFacingLine(player);
        }
    }

    public void Dispose()
    {
        if (_hasTexture)
        {
            Raylib.UnloadTexture(_texture);
        }
    }

    private void DrawSprite(Player player)
    {
        var source = new Rectangle(0, 0, _texture.Width, _texture.Height);
        var dest = new Rectangle(player.Position.X, player.Position.Y, SpriteSize, SpriteSize);
        float spriteRotation = player.RotationDegrees + SpriteFacesUpOffsetDegrees;
        Raylib.DrawTexturePro(_texture, source, dest, SpriteOrigin, spriteRotation, Color.White);
    }

    private static void DrawFallbackCircleWithFacingLine(Player player)
    {
        Raylib.DrawCircleV(player.Position, player.Radius, Color.SkyBlue);

        float radians = player.RotationDegrees * HeistConstants.DegToRad;
        Vector2 facingEnd = player.Position + (new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * FallbackAimLineLength);
        Raylib.DrawLineV(player.Position, facingEnd, Color.White);
    }
}
