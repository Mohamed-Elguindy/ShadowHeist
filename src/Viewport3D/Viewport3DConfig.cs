using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Centralized layout data, palette colors, and named tuning constants for the 3D tactical viewport.
/// </summary>
public static class Viewport3DConfig
{
    public const float MapWidth = 1280f;
    public const float MapHeight = 768f;
    public const float FloorTileSize = 256f;
    public const float FloorThickness = 4f;

    public const float WallHeight = 96f;
    public const float CutawayWallHeight = 14f;
    public const float WallCapHeight = 6f;
    public const float LeftCutawayMaxX = 48f;
    public const float BottomCutawayMinY = 680f;
    public const float PillarSize = 40f;

    public const float CharacterHeight = 72f;
    public const float CharacterUnitScale = 72f / 16f;
    public const float ShadowRadius = 19f;

    public const float VisionRange = 235f;
    public const float VisionHalfAngleDegrees = 32f;
    public const int VisionRayCount = 26;

    public const float CameraFieldOfView = 50f;
    public const float CameraFollowSpeed = 6.5f;
    public const float MinZoom = 0.7f;
    public const float MaxZoom = 1.65f;
    public const float ZoomStep = 0.1f;

    public static readonly Vector3 FollowCameraOffset = new(-180f, 390f, 320f);
    public static readonly Vector3 OverviewTarget = new(630f, 0f, 385f);
    public static readonly Vector3 OverviewCameraOffset = new(-105f, 500f, 340f);
    public static readonly Vector2 VaultZoneMin = new(320f, 160f);
    public static readonly Vector2 VaultZoneMax = new(960f, 416f);
    public static readonly Rectangle GetawayZoneBounds = new(64f, 640f, 128f, 96f);

    public static readonly Color SkyColor = new(12, 16, 26, 255);
    public static readonly Color AlarmSkyColor = new(34, 12, 18, 255);
    public static readonly Color WallTint = new(168, 178, 198, 255);
    public static readonly Color WallCapColor = new(102, 118, 144, 255);
    public static readonly Color OutlineColor = new(12, 15, 22, 255);
    public static readonly Color ShadowColor = new(0, 0, 0, 115);

    public static readonly IReadOnlyList<FurnitureSpec> LobbyFurniture = new FurnitureSpec[]
    {
        new("rugRectangle", new Vector2(165f, 440f), 90f, 165f),
        new("loungeSofa", new Vector2(96f, 440f), 90f, 105f),
        new("tableCoffee", new Vector2(182f, 440f), 90f, 72f),
        new("loungeChair", new Vector2(182f, 340f), 0f, 52f),
        new("loungeChair", new Vector2(182f, 540f), 180f, 52f),
        new("lampRoundFloor", new Vector2(84f, 345f), 0f, 82f, EmitsWarmGlow: true),
        new("pottedPlant", new Vector2(96f, 232f), 20f, 60f),
        new("pottedPlant", new Vector2(276f, 220f), -15f, 58f),
        new("plantSmall1", new Vector2(84f, 84f), 0f, 34f),
        new("sideTableDrawers", new Vector2(640f, 74f), 0f, 60f),
        new("plantSmall1", new Vector2(1188f, 84f), 45f, 34f),
        new("table", new Vector2(560f, 540f), 0f, 105f),
        new("loungeChair", new Vector2(560f, 616f), 180f, 52f),
        new("rugRectangle", new Vector2(805f, 575f), 0f, 165f),
        new("loungeSofa", new Vector2(805f, 632f), 180f, 105f),
        new("tableCoffee", new Vector2(805f, 550f), 0f, 72f),
        new("lampRoundFloor", new Vector2(896f, 632f), 0f, 82f, EmitsWarmGlow: true),
        new("pottedPlant", new Vector2(1175f, 650f), 30f, 60f),
        new("sideTableDrawers", new Vector2(1186f, 380f), -90f, 60f),
    };
}
