using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders multi-zone cel-shaded 3D bank floors, cutaway concrete walls, corner pillars, getaway pad, and furniture.
/// </summary>
public sealed class FloorWallsFurnitureRenderer : ISceneRenderPass
{
    private const string MarbleFloorPath = "assets/sprites/floor_marble_dark.png";
    private const string SlateFloorPath = "assets/sprites/floor_slate_corridor.png";
    private const string SteelFloorPath = "assets/sprites/floor_vault_steel.png";
    private const string WallTexturePath = "assets/sprites/wall_reinforced_concrete.png";
    private const string FurnitureFolder = "assets/models/furniture";
    private const string RugAssetKey = "rugRectangle";
    private const float WallPanelLength = 64f;
    private const float RugLiftY = 1.2f;
    private const float PropOutlineWidth = 0.012f;

    private static readonly Color CornerPillarColor = new(62, 72, 92, 255);
    private static readonly Color GetawayPadBorderColor = new(74, 222, 128, 220);

    private readonly Dictionary<string, LoadedModel> _furnitureModels = new();
    private ToonModelLibrary? _library;
    private Model? _marbleSlab;
    private Model? _slateSlab;
    private Model? _steelSlab;
    private Model? _wallUnitBox;

    public void Load(ToonModelLibrary library)
    {
        _library = library;
        float tileSize = Viewport3DConfig.FloorTileSize;
        float thickness = Viewport3DConfig.FloorThickness;

        _marbleSlab = library.LoadTexturedBox(MarbleFloorPath, tileSize, thickness, tileSize);
        _slateSlab = library.LoadTexturedBox(SlateFloorPath, tileSize, thickness, tileSize);
        _steelSlab = library.LoadTexturedBox(SteelFloorPath, tileSize, thickness, tileSize);
        _wallUnitBox = library.LoadTexturedBox(WallTexturePath, 1f, 1f, 1f);

        LoadFurnitureModels(library);
    }

    public void Draw(in Scene3DRenderFrame frame)
    {
        DrawFloorZones();
        DrawGetawayPad(frame.GetawayZone, frame.ElapsedSeconds);
        DrawWallsAndPillars(frame.ArchitecturalWalls);
        DrawFurniture();
    }

    private void LoadFurnitureModels(ToonModelLibrary library)
    {
        foreach (FurnitureSpec spec in BankFurnitureLayout.LobbyFurniture)
        {
            if (_furnitureModels.ContainsKey(spec.AssetKey))
            {
                continue;
            }

            string modelPath = Path.Combine(FurnitureFolder, $"{spec.AssetKey}.obj");
            if (library.LoadProp(modelPath, spec.TargetMaxExtent) is LoadedModel loaded)
            {
                _furnitureModels[spec.AssetKey] = loaded;
            }
        }
    }

    private void DrawFloorZones()
    {
        float tileStep = Viewport3DConfig.FloorTileSize;
        float centerY = -Viewport3DConfig.FloorThickness * 0.5f;

        for (float z = 0f; z < Viewport3DConfig.MapHeight; z += tileStep)
        {
            for (float x = 0f; x < Viewport3DConfig.MapWidth; x += tileStep)
            {
                Model? slab = SelectFloorSlab(x + (tileStep * 0.5f), z + (tileStep * 0.5f));
                if (slab is Model tileModel)
                {
                    Raylib.DrawModel(tileModel, new Vector3(x + (tileStep * 0.5f), centerY, z + (tileStep * 0.5f)), 1f, Color.White);
                }
            }
        }
    }

    private Model? SelectFloorSlab(float centerX, float centerZ)
    {
        bool insideVault = centerX >= Viewport3DConfig.VaultZoneMin.X
                        && centerX <= Viewport3DConfig.VaultZoneMax.X
                        && centerZ >= Viewport3DConfig.VaultZoneMin.Y
                        && centerZ <= Viewport3DConfig.VaultZoneMax.Y;

        if (insideVault)
        {
            return _steelSlab;
        }

        if (centerZ < Viewport3DConfig.VaultZoneMin.Y || centerX > Viewport3DConfig.VaultZoneMax.X)
        {
            return _slateSlab;
        }

        return _marbleSlab;
    }

    private static void DrawGetawayPad(RectangleBounds? getawayZone, float timeSeconds)
    {
        RectangleBounds zone = getawayZone ?? HeistConstants.DefaultGetawayZone;
        byte alpha = (byte)(110f * (0.65f + 0.35f * MathF.Sin(timeSeconds * 3f)));

        var center = new Vector3(zone.X + (zone.Width * 0.5f), 0.5f, zone.Y + (zone.Height * 0.5f));
        Raylib.DrawCube(center, zone.Width, 1f, zone.Height, new Color((byte)28, (byte)155, (byte)102, alpha));
        Raylib.DrawCubeWires(center, zone.Width, 1.5f, zone.Height, GetawayPadBorderColor);
    }

    private void DrawWallsAndPillars(IReadOnlyList<RectangleBounds> walls)
    {
        foreach (RectangleBounds wall in walls)
        {
            bool isCutaway = IsCutawayPerimeterWall(wall);
            float height = isCutaway ? Viewport3DConfig.CutawayWallHeight : Viewport3DConfig.WallHeight;
            DrawSegmentedWall(wall, height, isCutaway);
        }

        DrawCornerPillar(336f, 176f);
        DrawCornerPillar(336f, 400f);
        DrawCornerPillar(944f, 176f);
        DrawCornerPillar(944f, 400f);
    }

    private static bool IsCutawayPerimeterWall(RectangleBounds wall)
    {
        bool isWestOuter = wall.X < Viewport3DConfig.LeftCutawayMaxX && wall.Height > wall.Width;
        bool isSouthOuter = wall.Y > Viewport3DConfig.BottomCutawayMinY && wall.Width > wall.Height;
        return isWestOuter || isSouthOuter;
    }

    private void DrawSegmentedWall(RectangleBounds wall, float height, bool isCutaway)
    {
        bool splitAlongX = wall.Width >= wall.Height;
        float span = splitAlongX ? wall.Width : wall.Height;
        int segmentCount = Math.Max(1, (int)MathF.Ceiling(span / WallPanelLength));
        float segmentStep = span / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float segmentWidth = splitAlongX ? segmentStep : wall.Width;
            float segmentLength = splitAlongX ? wall.Height : segmentStep;
            float centerX = splitAlongX ? wall.X + segmentStep * (i + 0.5f) : wall.X + (wall.Width * 0.5f);
            float centerZ = splitAlongX ? wall.Y + (wall.Height * 0.5f) : wall.Y + segmentStep * (i + 0.5f);

            var center = new Vector3(centerX, height * 0.5f, centerZ);
            if (_wallUnitBox is Model boxModel)
            {
                Raylib.DrawModelEx(boxModel, center, Vector3.UnitY, 0f, new Vector3(segmentWidth, height, segmentLength), Viewport3DConfig.WallTint);
            }

            if (!isCutaway)
            {
                var capCenter = new Vector3(centerX, height + (Viewport3DConfig.WallCapHeight * 0.5f), centerZ);
                Raylib.DrawCube(capCenter, segmentWidth + 1f, Viewport3DConfig.WallCapHeight, segmentLength + 1f, Viewport3DConfig.WallCapColor);
            }

            Raylib.DrawCubeWires(center, segmentWidth, height, segmentLength, Viewport3DConfig.OutlineColor);
        }
    }

    private static void DrawCornerPillar(float x, float z)
    {
        float height = Viewport3DConfig.WallHeight + Viewport3DConfig.WallCapHeight + 2f;
        var center = new Vector3(x, height * 0.5f, z);
        var size = new Vector3(Viewport3DConfig.PillarSize, height, Viewport3DConfig.PillarSize);
        Raylib3DPrimitives.DrawOutlinedCube(center, size, CornerPillarColor, Viewport3DConfig.OutlineColor);
    }

    private void DrawFurniture()
    {
        if (_library is null)
        {
            return;
        }

        foreach (FurnitureSpec spec in BankFurnitureLayout.LobbyFurniture)
        {
            if (!_furnitureModels.TryGetValue(spec.AssetKey, out LoadedModel model))
            {
                continue;
            }

            bool isRug = spec.AssetKey == RugAssetKey;
            if (!isRug)
            {
                Raylib3DPrimitives.DrawGroundShadow(spec.Position, spec.TargetMaxExtent * 0.42f);
            }

            float verticalOffset = model.BaseOffsetY + (isRug ? RugLiftY : 0f);
            var worldPosition = new Vector3(spec.Position.X, verticalOffset, spec.Position.Y);
            float outlineWidth = isRug ? 0f : PropOutlineWidth;

            _library.DrawCenteredProp(model, worldPosition, spec.RotationDegrees, Color.White, outlineWidth);
        }
    }
}
