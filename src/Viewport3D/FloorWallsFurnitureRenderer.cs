using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders multi-zone cel-shaded 3D bank floors, cutaway concrete walls, pillars, and lobby furniture.
/// </summary>
public sealed class FloorWallsFurnitureRenderer
{
    private const string MarbleFloorPath = "assets/sprites/floor_marble_dark.png";
    private const string SlateFloorPath = "assets/sprites/floor_slate_corridor.png";
    private const string SteelFloorPath = "assets/sprites/floor_vault_steel.png";
    private const string WallTexturePath = "assets/sprites/wall_reinforced_concrete.png";
    private const string FurnitureFolder = "assets/models/furniture";
    private const float WallPanelLength = 128f;
    private const float RugLiftY = 1.2f;

    private readonly Dictionary<string, LoadedModel> _furniture = new();
    private Model? _marbleSlab;
    private Model? _slateSlab;
    private Model? _steelSlab;
    private Model? _wallUnitBox;

    public void Load(ToonModelLibrary library)
    {
        float tile = Viewport3DConfig.FloorTileSize;
        float thick = Viewport3DConfig.FloorThickness;
        _marbleSlab = library.LoadTexturedBox(MarbleFloorPath, tile, thick, tile);
        _slateSlab = library.LoadTexturedBox(SlateFloorPath, tile, thick, tile);
        _steelSlab = library.LoadTexturedBox(SteelFloorPath, tile, thick, tile);
        _wallUnitBox = library.LoadTexturedBox(WallTexturePath, 1f, 1f, 1f);

        foreach (FurnitureSpec spec in Viewport3DConfig.LobbyFurniture)
        {
            if (_furniture.ContainsKey(spec.AssetKey)) continue;
            string path = Path.Combine(FurnitureFolder, $"{spec.AssetKey}.obj");
            if (library.LoadProp(path, spec.TargetMaxExtent) is LoadedModel loaded)
                _furniture[spec.AssetKey] = loaded;
        }
    }

    public void Draw(IReadOnlyList<RectangleBounds> walls, float timeSeconds)
    {
        DrawFloorZones();
        DrawGetawayPad(timeSeconds);
        DrawWalls(walls);
        DrawFurniture();
    }

    private void DrawFloorZones()
    {
        float step = Viewport3DConfig.FloorTileSize;
        float y = -Viewport3DConfig.FloorThickness / 2f;
        for (float z = 0f; z < Viewport3DConfig.MapHeight; z += step)
            for (float x = 0f; x < Viewport3DConfig.MapWidth; x += step)
                if (SelectFloorSlab(x + step * 0.5f, z + step * 0.5f) is Model tileModel)
                    Raylib.DrawModel(tileModel, new Vector3(x + step / 2f, y, z + step / 2f), 1f, Color.White);

        Vector2 min = Viewport3DConfig.VaultZoneMin;
        Vector2 max = Viewport3DConfig.VaultZoneMax;
        Raylib.DrawCube(new Vector3((min.X + max.X) / 2f, 0.4f, max.Y), max.X - min.X, 0.8f, 5f, new Color(56, 189, 248, 120));
    }

    private Model? SelectFloorSlab(float cx, float cz)
    {
        bool inVault = cx >= Viewport3DConfig.VaultZoneMin.X && cx <= Viewport3DConfig.VaultZoneMax.X
                    && cz >= Viewport3DConfig.VaultZoneMin.Y && cz <= Viewport3DConfig.VaultZoneMax.Y;
        if (inVault) return _steelSlab;
        if (cz < Viewport3DConfig.VaultZoneMin.Y || cx > Viewport3DConfig.VaultZoneMax.X) return _slateSlab;
        return _marbleSlab;
    }

    private static void DrawGetawayPad(float timeSeconds)
    {
        Rectangle pad = Viewport3DConfig.GetawayZoneBounds;
        byte alpha = (byte)(110 * (0.65f + 0.35f * MathF.Sin(timeSeconds * 3f)));
        var center = new Vector3(pad.X + pad.Width / 2f, 0.5f, pad.Y + pad.Height / 2f);
        Raylib.DrawCube(center, pad.Width, 1f, pad.Height, new Color((byte)28, (byte)155, (byte)102, alpha));
        Raylib.DrawCubeWires(center, pad.Width, 1.5f, pad.Height, new Color(74, 222, 128, 220));
    }

    private void DrawWalls(IReadOnlyList<RectangleBounds> walls)
    {
        foreach (RectangleBounds wall in walls)
        {
            bool isCutaway = wall.X < Viewport3DConfig.LeftCutawayMaxX || wall.Y > Viewport3DConfig.BottomCutawayMinY;
            float height = isCutaway ? Viewport3DConfig.CutawayWallHeight : Viewport3DConfig.WallHeight;
            DrawSegmentedWall(wall, height, isCutaway);
        }

        DrawCornerPillar(336f, 176f);
        DrawCornerPillar(336f, 384f);
        DrawCornerPillar(960f, 176f);
    }

    private void DrawSegmentedWall(RectangleBounds wall, float height, bool isCutaway)
    {
        bool splitX = wall.Width >= wall.Height;
        int segments = Math.Max(1, (int)MathF.Ceiling((splitX ? wall.Width : wall.Height) / WallPanelLength));
        float step = (splitX ? wall.Width : wall.Height) / segments;

        for (int i = 0; i < segments; i++)
        {
            float segW = splitX ? step : wall.Width;
            float segL = splitX ? wall.Height : step;
            float segX = splitX ? wall.X + step * (i + 0.5f) : wall.X + wall.Width * 0.5f;
            float segZ = splitX ? wall.Y + wall.Height * 0.5f : wall.Y + step * (i + 0.5f);
            var center = new Vector3(segX, height * 0.5f, segZ);

            if (_wallUnitBox is Model box)
            {
                Raylib.DrawModelEx(box, center, Vector3.UnitY, 0f, new Vector3(segW, height, segL), Viewport3DConfig.WallTint);
            }

            if (!isCutaway)
            {
                var cap = new Vector3(segX, height + Viewport3DConfig.WallCapHeight * 0.5f, segZ);
                Raylib.DrawCube(cap, segW + 1f, Viewport3DConfig.WallCapHeight, segL + 1f, Viewport3DConfig.WallCapColor);
            }

            Raylib.DrawCubeWires(center, segW, height, segL, Viewport3DConfig.OutlineColor);
        }
    }

    private static void DrawCornerPillar(float x, float z)
    {
        float h = Viewport3DConfig.WallHeight + Viewport3DConfig.WallCapHeight + 2f;
        var pos = new Vector3(x, h * 0.5f, z);
        Raylib.DrawCube(pos, Viewport3DConfig.PillarSize, h, Viewport3DConfig.PillarSize, new Color(62, 72, 92, 255));
        Raylib.DrawCubeWires(pos, Viewport3DConfig.PillarSize, h, Viewport3DConfig.PillarSize, Viewport3DConfig.OutlineColor);
    }

    private void DrawFurniture()
    {
        foreach (FurnitureSpec spec in Viewport3DConfig.LobbyFurniture)
        {
            if (!_furniture.TryGetValue(spec.AssetKey, out LoadedModel item)) continue;
            float extraY = spec.AssetKey == "rugRectangle" ? RugLiftY : 0f;
            var pos = new Vector3(spec.Position.X, item.BaseOffsetY + extraY, spec.Position.Y);
            Raylib.DrawModelEx(item.Model, pos, Vector3.UnitY, spec.RotationDegrees, new Vector3(item.Scale), Color.White);
        }
    }
}
