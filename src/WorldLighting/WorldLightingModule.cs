using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Interfaces;

namespace ShadowHeist.WorldLighting;

/// <summary>
/// Owned by: Ali (ShadowHeist.WorldLighting)
/// Week 1: Ticket #7 (JSON MapLoader) &amp; Ticket #8 (CollisionHelper Wall Sliding)
/// Week 2: Ticket #15 (VisionRaycaster Cone) &amp; Ticket #16 (LaserTripwire)
/// Week 3: Ticket #23 (ShadowRenderer RenderTexture2D) &amp; Ticket #24 (BrokenGlassTile Hazard)
/// Week 4: Ticket #31 (Level 1 &amp; 2 JSON Maps) &amp; Ticket #32 (Bloom Shader + AudioManager)
/// </summary>
public sealed class WorldLightingModule : IGameSystem, IDisposable
{
    private const string Level1MapPath = "assets/maps/level1_city_bank.json";
    private const string FloorTexturePath = "assets/sprites/floor_marble_dark.png";
    private const string WallTexturePath = "assets/sprites/wall_reinforced_concrete.png";

    private static readonly Color FallbackFloorColorEven = new(20, 26, 36, 255);
    private static readonly Color FallbackFloorColorOdd = new(16, 21, 30, 255);
    private static readonly Color FallbackWallColor = new(58, 66, 82, 255);
    private static readonly Color FallbackWallOutlineColor = new(88, 98, 118, 255);

    private LevelMapData? _currentMap;
    private Texture2D _floorTexture;
    private Texture2D _wallTexture;
    private bool _floorTextureLoaded;
    private bool _wallTextureLoaded;

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

        // TODO (Ticket #15 & #16 - Ali): Update VisionRaycaster and LaserTripwire beams
    }

    public void Draw(GameContext context)
    {
        DrawFloorTiles();
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
        if (_currentMap is null) return;

        int tileSize = _currentMap.TileSize;
        var sourceRect = _floorTextureLoaded
            ? new Rectangle(0f, 0f, _floorTexture.Width, _floorTexture.Height)
            : default;

        for (int row = 0; row < _currentMap.GridHeight; row++)
        {
            for (int col = 0; col < _currentMap.GridWidth; col++)
            {
                float x = col * tileSize;
                float y = row * tileSize;

                if (_floorTextureLoaded)
                {
                    var destRect = new Rectangle(x, y, tileSize, tileSize);
                    Raylib.DrawTexturePro(_floorTexture, sourceRect, destRect, Vector2.Zero, 0f, Color.White);
                }
                else
                {
                    Color tileColor = (row + col) % 2 == 0 ? FallbackFloorColorEven : FallbackFloorColorOdd;
                    Raylib.DrawRectangle((int)x, (int)y, tileSize, tileSize, tileColor);
                }
            }
        }
    }

    private void DrawWalls(IReadOnlyList<RectangleBounds> walls)
    {
        var sourceRect = _wallTextureLoaded
            ? new Rectangle(0f, 0f, _wallTexture.Width, _wallTexture.Height)
            : default;

        foreach (var wall in walls)
        {
            if (_wallTextureLoaded)
            {
                var destRect = new Rectangle(wall.X, wall.Y, wall.Width, wall.Height);
                Raylib.DrawTexturePro(_wallTexture, sourceRect, destRect, Vector2.Zero, 0f, Color.White);
            }
            else
            {
                Raylib.DrawRectangle((int)wall.X, (int)wall.Y, (int)wall.Width, (int)wall.Height, FallbackWallColor);
            }

            Raylib.DrawRectangleLines((int)wall.X, (int)wall.Y, (int)wall.Width, (int)wall.Height, FallbackWallOutlineColor);
        }
    }
}
