using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Centralized tuning constants, camera settings, and palette colors for the 3D tactical viewport.
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
    public const float CharacterUnitScale = CharacterHeight / 16f;
    public const float ShadowRadius = 19f;

    public const float VisionRange = 235f;
    public const float VisionHalfAngleDegrees = 32f;
    public const int VisionRayCount = 26;

    public const float FollowFovDegrees = 50f;
    public const float OverviewFovDegrees = 62f;
    public const float CameraFollowSpeed = 6.5f;
    public const float FollowOverviewBlend = 0.22f;
    public const float MinZoom = 0.7f;
    public const float MaxZoom = 1.65f;
    public const float ZoomStep = 0.1f;

    public static readonly Vector3 FollowCameraOffset = new(-200f, 410f, 350f);
    public static readonly Vector3 OverviewTarget = new(630f, 0f, 385f);
    public static readonly Vector3 OverviewCameraOffset = new(-105f, 500f, 340f);
    public static readonly Vector2 VaultZoneMin = new(320f, 160f);
    public static readonly Vector2 VaultZoneMax = new(960f, 416f);
    public static readonly Vector2[] WarmLampPositions = [new(84f, 344f), new(912f, 646f)];

    public static readonly Color SkyColor = new(12, 16, 26, 255);
    public static readonly Color AlarmSkyColor = new(34, 12, 18, 255);
    public static readonly Color WallTint = new(168, 178, 198, 255);
    public static readonly Color WallCapColor = new(102, 118, 144, 255);
    public static readonly Color OutlineColor = new(12, 15, 22, 255);
    public static readonly Color ShadowColor = new(0, 0, 0, 115);
}
