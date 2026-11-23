using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders a grounded midnight city street, perimeter sidewalk curb, streetlamps, bollards, and exterior alarm strobes.
/// </summary>
internal static class ExteriorStreet3DRenderer
{
    private const float AsphaltCenterY = -3.8f;
    private const float AsphaltThickness = 2.0f;
    private const float AsphaltTopY = AsphaltCenterY + (AsphaltThickness * 0.5f);
    private const float SidewalkCenterY = -2.0f;
    private const float SidewalkThickness = 2.0f;
    private const float SidewalkTopY = SidewalkCenterY + (SidewalkThickness * 0.5f);
    private const float LaneLineY = -2.7f;
    private const float LaneLineThickness = 0.2f;
    private const float LaneDashLength = 34f;
    private const float LaneDashStride = 68f;
    private const float LaneDashWidth = 3f;

    private const float SouthAvenueMinX = -260f;
    private const float SouthAvenueMaxX = 1540f;
    private const float SouthAvenueStartZ = 792f;
    private const float SouthAvenueDepth = 210f;
    private const float SouthLaneCenterZ = 896f;

    private const float WestAlleyWidth = 212f;
    private const float WestAlleyStartX = -260f;
    private const float WestAlleyStartZ = -140f;
    private const float WestAlleyLength = 932f;
    private const float WestLaneCenterX = -154f;

    private const float SidewalkWidthSouth = 24f;
    private const float SidewalkWidthSide = 48f;
    private const float CurbHeight = 0.6f;
    private const float CurbWidth = 2f;
    private const float CurbY = -1.1f;

    private const float FarBlockStartZ = 1002f;
    private const float FarCurbDepth = 36f;
    private const float FarBlockDepth = 110f;
    private const float FarBlockHeight = 14f;

    private const float BollardHeight = 14f;
    private const float BollardSize = 5f;
    private const float BollardCapHeight = 2f;
    private const float BollardZ = 786f;

    private const float LampHeight = 68f;
    private const float LampPostSize = 4f;
    private const float LampArmLength = 20f;
    private const float LampBulbInsetY = 2f;
    private const float LampPoolY = -2.65f;
    private const float LampOuterPoolRadius = 86f;
    private const float LampInnerPoolRadius = 40f;
    private const float LampPoolHeight = 0.3f;
    private const int LampPoolSlices = 18;
    private const float LampBulbRadius = 4.2f;
    private const float StrobeHeightY = 104f;
    private const float StrobeRadius = 5.5f;
    private const float StrobePulseSpeed = 10f;

    private static readonly float[] SouthBollardPositionsX = { 300f, 520f, 760f, 1080f };
    private static readonly Vector2[] StreetlampSpecs = { new(410f, 782f), new(960f, 782f), new(-28f, 340f) };
    private static readonly Vector2[] ExteriorStrobePositions = { new(24f, 744f), new(1256f, 744f) };

    private static readonly Color AsphaltColor = new(13, 17, 26, 255);
    private static readonly Color SidewalkColor = new(24, 30, 44, 255);
    private static readonly Color CurbColor = new(44, 55, 76, 255);
    private static readonly Color LaneDashColor = new(234, 179, 8, 95);
    private static readonly Color FarBlockColor = new(16, 21, 33, 255);
    private static readonly Color BollardColor = new(48, 58, 78, 255);
    private static readonly Color BollardReflectorColor = new(245, 197, 66, 210);
    private static readonly Color LampPoleColor = new(36, 44, 60, 255);
    private static readonly Color LampBulbColor = new(255, 228, 156, 235);
    private static readonly Color LampOuterPoolColor = new(255, 206, 112, 24);
    private static readonly Color LampInnerPoolColor = new(255, 220, 140, 42);
    private static readonly Color StrobeRedColor = new(244, 63, 94, 230);
    private static readonly Color StrobeBlueColor = new(56, 189, 248, 230);

    public static void Draw(float elapsedSeconds, bool isAlarmTriggered)
    {
        DrawAsphaltRoads();
        DrawPerimeterSidewalk();
        DrawDashedRoadMarkings();
        DrawFarSidewalkSilhouette();
        DrawCurbBollards();
        DrawStreetlamps();
        if (isAlarmTriggered)
        {
            DrawExteriorAlarmStrobes(elapsedSeconds);
        }
    }

    private static void DrawAsphaltRoads()
    {
        float southWidth = SouthAvenueMaxX - SouthAvenueMinX;
        float southCenterX = SouthAvenueMinX + (southWidth * 0.5f);
        Raylib.DrawCube(new Vector3(southCenterX, AsphaltCenterY, SouthAvenueStartZ + (SouthAvenueDepth * 0.5f)), southWidth, AsphaltThickness, SouthAvenueDepth, AsphaltColor);
        Raylib.DrawCube(new Vector3(WestAlleyStartX + (WestAlleyWidth * 0.5f), AsphaltCenterY, WestAlleyStartZ + (WestAlleyLength * 0.5f)), WestAlleyWidth, AsphaltThickness, WestAlleyLength, AsphaltColor);
    }

    private static void DrawPerimeterSidewalk()
    {
        float southSpan = Viewport3DConfig.MapWidth + (SidewalkWidthSide * 2f);
        float centerX = Viewport3DConfig.MapWidth * 0.5f;
        float centerZ = Viewport3DConfig.MapHeight * 0.5f;

        Raylib.DrawCube(new Vector3(centerX, SidewalkCenterY, Viewport3DConfig.MapHeight + (SidewalkWidthSouth * 0.5f)), southSpan, SidewalkThickness, SidewalkWidthSouth, SidewalkColor);
        Raylib.DrawCube(new Vector3(centerX, SidewalkCenterY, -SidewalkWidthSide * 0.5f), southSpan, SidewalkThickness, SidewalkWidthSide, SidewalkColor);
        Raylib.DrawCube(new Vector3(-SidewalkWidthSide * 0.5f, SidewalkCenterY, centerZ), SidewalkWidthSide, SidewalkThickness, Viewport3DConfig.MapHeight, SidewalkColor);
        Raylib.DrawCube(new Vector3(Viewport3DConfig.MapWidth + (SidewalkWidthSide * 0.5f), SidewalkCenterY, centerZ), SidewalkWidthSide, SidewalkThickness, Viewport3DConfig.MapHeight, SidewalkColor);

        Raylib.DrawCube(new Vector3(centerX, CurbY, SouthAvenueStartZ), southSpan, CurbHeight, CurbWidth, CurbColor);
        Raylib.DrawCube(new Vector3(-SidewalkWidthSide, CurbY, centerZ), CurbWidth, CurbHeight, Viewport3DConfig.MapHeight + SidewalkWidthSide, CurbColor);
    }

    private static void DrawDashedRoadMarkings()
    {
        for (float x = SouthAvenueMinX + LaneDashLength; x < SouthAvenueMaxX - LaneDashLength; x += LaneDashStride)
        {
            Raylib.DrawCube(new Vector3(x, LaneLineY, SouthLaneCenterZ), LaneDashLength, LaneLineThickness, LaneDashWidth, LaneDashColor);
        }

        for (float z = WestAlleyStartZ + LaneDashLength; z < SouthAvenueStartZ - LaneDashLength; z += LaneDashStride)
        {
            Raylib.DrawCube(new Vector3(WestLaneCenterX, LaneLineY, z), LaneDashWidth, LaneLineThickness, LaneDashLength, LaneDashColor);
        }
    }

    private static void DrawFarSidewalkSilhouette()
    {
        float span = SouthAvenueMaxX - SouthAvenueMinX;
        float centerX = SouthAvenueMinX + (span * 0.5f);
        Raylib.DrawCube(new Vector3(centerX, SidewalkCenterY, FarBlockStartZ + (FarCurbDepth * 0.5f)), span, SidewalkThickness, FarCurbDepth, SidewalkColor);
        Raylib.DrawCube(new Vector3(centerX, AsphaltTopY + (FarBlockHeight * 0.5f), FarBlockStartZ + FarCurbDepth + (FarBlockDepth * 0.5f)), span, FarBlockHeight, FarBlockDepth, FarBlockColor);
    }

    private static void DrawCurbBollards()
    {
        float baseY = SidewalkTopY + (BollardHeight * 0.5f);
        foreach (float x in SouthBollardPositionsX)
        {
            Raylib3DPrimitives.DrawOutlinedCube(new Vector3(x, baseY, BollardZ), new Vector3(BollardSize, BollardHeight, BollardSize), BollardColor, Viewport3DConfig.OutlineColor);
            Raylib.DrawCube(new Vector3(x, SidewalkTopY + BollardHeight + (BollardCapHeight * 0.5f), BollardZ), BollardSize, BollardCapHeight, BollardSize, BollardReflectorColor);
        }
    }

    private static void DrawStreetlamps()
    {
        foreach (Vector2 pos in StreetlampSpecs)
        {
            bool isWestAlley = pos.X < 0f;
            float armOffsetX = isWestAlley ? -LampArmLength * 0.5f : 0f;
            float armOffsetZ = isWestAlley ? 0f : LampArmLength * 0.5f;
            Vector3 bulbPos = new(pos.X + (armOffsetX * 2f), LampHeight - LampBulbInsetY, pos.Y + (armOffsetZ * 2f));

            Raylib.DrawCube(new Vector3(pos.X, LampHeight * 0.5f, pos.Y), LampPostSize, LampHeight, LampPostSize, LampPoleColor);
            Raylib.DrawCube(new Vector3(pos.X + armOffsetX, LampHeight, pos.Y + armOffsetZ), isWestAlley ? LampArmLength : LampPostSize, LampPostSize, isWestAlley ? LampPostSize : LampArmLength, LampPoleColor);
            Raylib.DrawSphere(bulbPos, LampBulbRadius, LampBulbColor);

            var poolCenter = new Vector3(bulbPos.X, LampPoolY, bulbPos.Z);
            Raylib.DrawCylinder(poolCenter, LampOuterPoolRadius, LampOuterPoolRadius, LampPoolHeight, LampPoolSlices, LampOuterPoolColor);
            Raylib.DrawCylinder(poolCenter, LampInnerPoolRadius, LampInnerPoolRadius, LampPoolHeight, LampPoolSlices, LampInnerPoolColor);
        }
    }

    private static void DrawExteriorAlarmStrobes(float elapsedSeconds)
    {
        bool showRed = MathF.Sin(elapsedSeconds * StrobePulseSpeed) >= 0f;
        for (int i = 0; i < ExteriorStrobePositions.Length; i++)
        {
            Vector2 pos = ExteriorStrobePositions[i];
            bool useRed = (i % 2 == 0) ? showRed : !showRed;
            Raylib.DrawSphere(new Vector3(pos.X, StrobeHeightY, pos.Y), StrobeRadius, useRed ? StrobeRedColor : StrobeBlueColor);
        }
    }
}
