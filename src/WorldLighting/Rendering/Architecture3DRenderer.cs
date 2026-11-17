using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders multi-zone cel-shaded 3D bank floors, cutaway concrete walls, corner pillars, and getaway pad.
/// </summary>
public sealed class Architecture3DRenderer
{
    private const string MarbleFloorPath = "assets/sprites/floor_marble_dark.png";
    private const string SlateFloorPath = "assets/sprites/floor_slate_corridor.png";
    private const string SteelFloorPath = "assets/sprites/floor_vault_steel.png";
    private const string WallTexturePath = "assets/sprites/wall_reinforced_concrete.png";
    private const float WallPanelLength = 64f;
    private const float OuterBorderMargin = 48f;
    private const float MaxOcclusionPlayerDistance = 135f;
    private const byte OccludedWallAlpha = 60;
    private const byte OccludedOutlineAlpha = 90;
    private const float GetawayPulseMaxAlpha = 110f;
    private const float GetawayPulseBase = 0.65f;
    private const float GetawayPulseAmplitude = 0.35f;
    private const float GetawayPulseSpeed = 3f;
    private const float GetawayPadFillHeight = 1f;
    private const float GetawayPadWireHeight = 1.5f;
    private const float VaultWestPillarX = 336f;
    private const float VaultEastPillarX = 944f;
    private const float VaultNorthPillarZ = 176f;
    private const float VaultSouthPillarZ = 400f;
    private const float PillarHeightPadding = 2f;

    private static readonly Color CornerPillarColor = new(62, 72, 92, 255);
    private static readonly Color GetawayPadBorderColor = new(74, 222, 128, 220);

    private Model? _marbleSlab;
    private Model? _slateSlab;
    private Model? _steelSlab;
    private Model? _wallUnitBox;

    public void Load(ToonModelLibrary library)
    {
        float tileSize = Viewport3DConfig.FloorTileSize;
        float thickness = Viewport3DConfig.FloorThickness;

        _marbleSlab = library.LoadTexturedBox(MarbleFloorPath, tileSize, thickness, tileSize);
        _slateSlab = library.LoadTexturedBox(SlateFloorPath, tileSize, thickness, tileSize);
        _steelSlab = library.LoadTexturedBox(SteelFloorPath, tileSize, thickness, tileSize);
        _wallUnitBox = library.LoadTexturedBox(WallTexturePath, 1f, 1f, 1f);
    }

    public void Draw(IReadOnlyList<RectangleBounds> architecturalWalls, RectangleBounds? getawayZone, float elapsedSeconds, Vector2 playerPosition, Vector3 cameraPosition)
    {
        DrawFloorZones();
        DrawGetawayPad(getawayZone, elapsedSeconds);
        DrawWallsAndPillars(architecturalWalls, playerPosition, new Vector2(cameraPosition.X, cameraPosition.Z));
    }

    private void DrawFloorZones()
    {
        float tileStep = Viewport3DConfig.FloorTileSize;
        float halfTile = tileStep * 0.5f;
        float centerY = -Viewport3DConfig.FloorThickness * 0.5f;

        for (float z = 0f; z < Viewport3DConfig.MapHeight; z += tileStep)
        {
            for (float x = 0f; x < Viewport3DConfig.MapWidth; x += tileStep)
            {
                Model? slab = SelectFloorSlab(x + halfTile, z + halfTile);
                if (slab is Model tileModel)
                {
                    Raylib.DrawModel(tileModel, new Vector3(x + halfTile, centerY, z + halfTile), 1f, Color.White);
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
        byte alpha = (byte)(GetawayPulseMaxAlpha * (GetawayPulseBase + GetawayPulseAmplitude * MathF.Sin(timeSeconds * GetawayPulseSpeed)));
        var center = new Vector3(zone.X + (zone.Width * 0.5f), GetawayPadFillHeight * 0.5f, zone.Y + (zone.Height * 0.5f));

        Raylib.DrawCube(center, zone.Width, GetawayPadFillHeight, zone.Height, new Color((byte)28, (byte)155, (byte)102, alpha));
        Raylib.DrawCubeWires(center, zone.Width, GetawayPadWireHeight, zone.Height, GetawayPadBorderColor);
    }

    private void DrawWallsAndPillars(IReadOnlyList<RectangleBounds> walls, Vector2 playerPos, Vector2 cameraPos)
    {
        foreach (RectangleBounds wall in walls)
        {
            bool isCutaway = IsCutawayPerimeterWall(wall);
            float height = isCutaway ? Viewport3DConfig.CutawayWallHeight : Viewport3DConfig.WallHeight;
            bool isOccluding = !isCutaway
                && !IsOuterPerimeterWall(wall)
                && Geometry2D.DistanceToBounds(playerPos, wall) <= MaxOcclusionPlayerDistance
                && Geometry2D.SegmentIntersectsBounds(cameraPos, playerPos, wall);
            DrawSegmentedWall(wall, height, isCutaway, isOccluding);
        }

        DrawCornerPillar(VaultWestPillarX, VaultNorthPillarZ);
        DrawCornerPillar(VaultWestPillarX, VaultSouthPillarZ);
        DrawCornerPillar(VaultEastPillarX, VaultNorthPillarZ);
        DrawCornerPillar(VaultEastPillarX, VaultSouthPillarZ);
    }

    private static bool IsCutawayPerimeterWall(RectangleBounds wall)
    {
        bool isWestOuter = wall.X < Viewport3DConfig.LeftCutawayMaxX && wall.Height > wall.Width;
        bool isSouthOuter = wall.Y > Viewport3DConfig.BottomCutawayMinY && wall.Width > wall.Height;
        return isWestOuter || isSouthOuter;
    }

    private static bool IsOuterPerimeterWall(RectangleBounds wall) =>
        wall.X <= OuterBorderMargin
        || wall.Y <= OuterBorderMargin
        || wall.X + wall.Width >= Viewport3DConfig.MapWidth - OuterBorderMargin
        || wall.Y + wall.Height >= Viewport3DConfig.MapHeight - OuterBorderMargin;

    private void DrawSegmentedWall(RectangleBounds wall, float height, bool isCutaway, bool isOccluding)
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

            DrawSingleWallSegment(new Vector3(centerX, height * 0.5f, centerZ), new Vector3(segmentWidth, height, segmentLength), isCutaway, isOccluding);
        }
    }

    private void DrawSingleWallSegment(Vector3 center, Vector3 dimensions, bool isCutaway, bool isOccluding)
    {
        Color wallTint = isOccluding ? WithAlpha(Viewport3DConfig.WallTint, OccludedWallAlpha) : Viewport3DConfig.WallTint;
        Color capTint = isOccluding ? WithAlpha(Viewport3DConfig.WallCapColor, OccludedWallAlpha) : Viewport3DConfig.WallCapColor;
        Color wireTint = isOccluding ? WithAlpha(Viewport3DConfig.OutlineColor, OccludedOutlineAlpha) : Viewport3DConfig.OutlineColor;

        if (isOccluding) Rlgl.DisableDepthMask();
        if (_wallUnitBox is Model boxModel)
        {
            Raylib.DrawModelEx(boxModel, center, Vector3.UnitY, 0f, dimensions, wallTint);
        }

        if (!isCutaway)
        {
            var capCenter = new Vector3(center.X, dimensions.Y + (Viewport3DConfig.WallCapHeight * 0.5f), center.Z);
            Raylib.DrawCube(capCenter, dimensions.X + 1f, Viewport3DConfig.WallCapHeight, dimensions.Z + 1f, capTint);
        }

        Raylib.DrawCubeWires(center, dimensions.X, dimensions.Y, dimensions.Z, wireTint);
        if (isOccluding) Rlgl.EnableDepthMask();
    }

    private static Color WithAlpha(Color color, byte alpha) => new(color.R, color.G, color.B, alpha);

    private static void DrawCornerPillar(float x, float z)
    {
        float height = Viewport3DConfig.WallHeight + Viewport3DConfig.WallCapHeight + PillarHeightPadding;
        var center = new Vector3(x, height * 0.5f, z);
        var size = new Vector3(Viewport3DConfig.PillarSize, height, Viewport3DConfig.PillarSize);
        Raylib3DPrimitives.DrawOutlinedCube(center, size, CornerPillarColor, Viewport3DConfig.OutlineColor);
    }
}
