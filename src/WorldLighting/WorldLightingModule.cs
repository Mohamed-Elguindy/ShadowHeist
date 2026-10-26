using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.WorldLighting;

public sealed class WorldLightingModule : IGameSystem, IDisposable
{
    private const string Level1MapPath = "assets/maps/level1_city_bank.json";
    private const string FloorTexturePath = "assets/sprites/floor_marble_dark.png";
    private const string WallTexturePath = "assets/sprites/wall_reinforced_concrete.png";
    private const float WallTileSegmentSize = 64f;

    private static readonly Color FallbackFloorColorEven = new(20, 26, 36, 255);
    private static readonly Color FallbackFloorColorOdd = new(16, 21, 30, 255);
    private static readonly Color FallbackWallColor = new(58, 66, 82, 255);
    private static readonly Color FallbackWallOutlineColor = new(88, 98, 118, 255);
    private static readonly Color GetawayZoneFillColor = new(24, 160, 98, 36);
    private static readonly Color GetawayZoneBorderColor = new(46, 213, 115, 140);

    private LevelMapData? _currentMap;
    private Texture2D _floorTexture;
    private Texture2D _wallTexture;
    private bool _floorTextureLoaded;
    private bool _wallTextureLoaded;

    public LevelMapData? CurrentMap => _currentMap;

    public void Init(GameContext context)
    {
        _currentMap = MapLoader.LoadFromFile(Level1MapPath, context);
        LoadTextures();
    }

    public void Update(float deltaTime, GameContext context)
    {
        context.PlayerPosition = CollisionHelper.ResolveWallCollision(
            context.PlayerPosition,
            CollisionHelper.DefaultActorRadius,
            context.Walls);
    }

    public void Draw(GameContext context)
    {
        DrawFloorTiles();
        DrawGetawayZone();
        DrawWalls(context.Walls);
    }

    public void Dispose()
    {
        if (_floorTextureLoaded)
        {
            Raylib.UnloadTexture(_floorTexture);
            _floorTextureLoaded = false;
        }

        if (_wallTextureLoaded)
        {
            Raylib.UnloadTexture(_wallTexture);
            _wallTextureLoaded = false;
        }
    }

    private void LoadTextures()
    {
        if (File.Exists(FloorTexturePath))
        {
            _floorTexture = Raylib.LoadTexture(FloorTexturePath);
            _floorTextureLoaded = true;
        }

        if (File.Exists(WallTexturePath))
        {
            _wallTexture = Raylib.LoadTexture(WallTexturePath);
            _wallTextureLoaded = true;
        }
    }

    private void DrawFloorTiles()
    {
        if (_currentMap is null)
        {
            return;
        }

        int tileSize = _currentMap.TileSize;
        var sourceRect = _floorTextureLoaded
            ? new Rectangle(0f, 0f, _floorTexture.Width, _floorTexture.Height)
            : default;

        for (int row = 0; row < _currentMap.GridHeight; row++)
        {
            for (int col = 0; col < _currentMap.GridWidth; col++)
            {
                DrawSingleFloorTile(row, col, tileSize, sourceRect);
            }
        }
    }

    private void DrawSingleFloorTile(int row, int col, int tileSize, Rectangle sourceRect)
    {
        float x = col * tileSize;
        float y = row * tileSize;

        if (_floorTextureLoaded)
        {
            var destRect = new Rectangle(x, y, tileSize, tileSize);
            Raylib.DrawTexturePro(_floorTexture, sourceRect, destRect, Vector2.Zero, 0f, Color.White);
            return;
        }

        Color tileColor = (row + col) % 2 == 0 ? FallbackFloorColorEven : FallbackFloorColorOdd;
        Raylib.DrawRectangle((int)x, (int)y, tileSize, tileSize, tileColor);
    }

    private void DrawGetawayZone()
    {
        if (_currentMap is null)
        {
            return;
        }

        RectangleBounds zone = _currentMap.GetawayZone;
        Raylib.DrawRectangle((int)zone.X, (int)zone.Y, (int)zone.Width, (int)zone.Height, GetawayZoneFillColor);
        Raylib.DrawRectangleLines((int)zone.X, (int)zone.Y, (int)zone.Width, (int)zone.Height, GetawayZoneBorderColor);
    }

    private void DrawWalls(IReadOnlyList<RectangleBounds> walls)
    {
        foreach (var wall in walls)
        {
            if (_wallTextureLoaded)
            {
                DrawTiledWallSprite(wall);
            }
            else
            {
                Raylib.DrawRectangle((int)wall.X, (int)wall.Y, (int)wall.Width, (int)wall.Height, FallbackWallColor);
            }

            Raylib.DrawRectangleLines((int)wall.X, (int)wall.Y, (int)wall.Width, (int)wall.Height, FallbackWallOutlineColor);
        }
    }

    private void DrawTiledWallSprite(RectangleBounds wall)
    {
        for (float y = wall.Y; y < wall.Y + wall.Height; y += WallTileSegmentSize)
        {
            float segmentHeight = MathF.Min(WallTileSegmentSize, (wall.Y + wall.Height) - y);
            float sourceHeight = _wallTexture.Height * (segmentHeight / WallTileSegmentSize);

            for (float x = wall.X; x < wall.X + wall.Width; x += WallTileSegmentSize)
            {
                float segmentWidth = MathF.Min(WallTileSegmentSize, (wall.X + wall.Width) - x);
                float sourceWidth = _wallTexture.Width * (segmentWidth / WallTileSegmentSize);

                var sourceRect = new Rectangle(0f, 0f, sourceWidth, sourceHeight);
                var destRect = new Rectangle(x, y, segmentWidth, segmentHeight);
                Raylib.DrawTexturePro(_wallTexture, sourceRect, destRect, Vector2.Zero, 0f, Color.White);
            }
        }
    }
}
