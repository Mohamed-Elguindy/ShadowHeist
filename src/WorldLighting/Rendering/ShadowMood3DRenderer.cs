using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders dynamic 3D floor darkness overlays, wall-base ambient shadow strips, and emissive keypad/security glow accents.
/// </summary>
public sealed class ShadowMood3DRenderer
{
    private const float HalfScale = 0.5f;
    private const float FloorShadowVeilElevationY = 0.12f;
    private const float VaultFloorVeilElevationY = 0.18f;
    private const float FloorShadowVeilHeight = 0.08f;
    private const float WallSkirtElevationY = 0.16f;
    private const float WallSkirtHeight = 0.10f;
    private const float WallSkirtPadding = 10f;
    private const float MaxFloorVeilAlpha = 75f;
    private const float MaxVaultFloorAlpha = 95f;
    private const float MaxEmergencyVeilAlpha = 44f;
    private const float KeypadGlowElevationY = 0.55f;
    private const float KeypadWallBeaconY = 36f;
    private const float KeypadPoolOuterRadius = 38f;
    private const float KeypadPoolInnerRadius = 18f;
    private const float KeypadPoolHeight = 0.35f;
    private const int KeypadPoolSlices = 18;
    private const float KeypadBeaconRadius = 3.2f;
    private const float KeypadPulseSpeed = 4.2f;
    private const float KeypadPulseBase = 0.65f;
    private const float KeypadPulseAmplitude = 0.35f;
    private const float KeypadOuterBaseAlpha = 36f;
    private const float KeypadInnerBaseAlpha = 64f;
    private const float KeypadBeaconBaseAlpha = 175f;
    private const float KeypadBeaconPulseAlpha = 70f;

    private static readonly Color FloorVeilBaseColor = new(6, 10, 20, 255);
    private static readonly Color VaultVeilBaseColor = new(8, 12, 24, 255);
    private static readonly Color EmergencyVeilBaseColor = new(220, 38, 68, 255);
    private static readonly Color WallSkirtShadowColor = new(6, 9, 16, 64);
    private static readonly Color KeypadNormalOuterBaseColor = new(56, 225, 255, 255);
    private static readonly Color KeypadNormalInnerBaseColor = new(74, 222, 128, 255);
    private static readonly Color KeypadNormalBeaconBaseColor = new(110, 242, 185, 255);
    private static readonly Color KeypadAlarmOuterBaseColor = new(244, 63, 94, 255);
    private static readonly Color KeypadAlarmInnerBaseColor = new(255, 96, 116, 255);
    private static readonly Color KeypadAlarmBeaconBaseColor = new(255, 80, 100, 255);

    public void Draw(
        ShadowLightingController lightingController,
        IReadOnlyList<RectangleBounds> architecturalWalls,
        IReadOnlyList<Vector2> keypadPositions,
        float elapsedSeconds)
    {
        Rlgl.DisableDepthMask();
        DrawZoneDarknessVeils(lightingController);
        DrawWallAmbientShadowSkirts(architecturalWalls);
        DrawEmissiveKeypadAccents(lightingController, keypadPositions, elapsedSeconds);
        Rlgl.EnableDepthMask();
    }

    private static void DrawZoneDarknessVeils(ShadowLightingController controller)
    {
        byte floorAlpha = (byte)Math.Clamp(controller.AmbientDarknessIntensity * MaxFloorVeilAlpha, 0f, MaxFloorVeilAlpha);
        var floorCenter = new Vector3(
            Viewport3DConfig.MapWidth * HalfScale,
            FloorShadowVeilElevationY,
            Viewport3DConfig.MapHeight * HalfScale);

        Raylib.DrawCube(
            floorCenter,
            Viewport3DConfig.MapWidth,
            FloorShadowVeilHeight,
            Viewport3DConfig.MapHeight,
            WithAlpha(FloorVeilBaseColor, floorAlpha));

        Vector2 vaultCenter2D = (Viewport3DConfig.VaultZoneMin + Viewport3DConfig.VaultZoneMax) * HalfScale;
        Vector2 vaultSize2D = Viewport3DConfig.VaultZoneMax - Viewport3DConfig.VaultZoneMin;
        float vaultDarkness = controller.GetZoneDarkness(vaultCenter2D);
        byte vaultAlpha = (byte)Math.Clamp(vaultDarkness * MaxVaultFloorAlpha, 0f, MaxVaultFloorAlpha);

        Raylib.DrawCube(
            new Vector3(vaultCenter2D.X, VaultFloorVeilElevationY, vaultCenter2D.Y),
            vaultSize2D.X,
            FloorShadowVeilHeight,
            vaultSize2D.Y,
            WithAlpha(VaultVeilBaseColor, vaultAlpha));

        if (controller.EmergencyPulseIntensity > 0f)
        {
            byte emergencyAlpha = (byte)Math.Clamp(controller.EmergencyPulseIntensity * MaxEmergencyVeilAlpha, 0f, MaxEmergencyVeilAlpha);
            Raylib.DrawCube(
                floorCenter,
                Viewport3DConfig.MapWidth,
                FloorShadowVeilHeight,
                Viewport3DConfig.MapHeight,
                WithAlpha(EmergencyVeilBaseColor, emergencyAlpha));
        }
    }

    private static void DrawWallAmbientShadowSkirts(IReadOnlyList<RectangleBounds> walls)
    {
        foreach (RectangleBounds wall in walls)
        {
            var center = new Vector3(
                wall.X + (wall.Width * HalfScale),
                WallSkirtElevationY,
                wall.Y + (wall.Height * HalfScale));

            Raylib.DrawCube(
                center,
                wall.Width + WallSkirtPadding,
                WallSkirtHeight,
                wall.Height + WallSkirtPadding,
                WallSkirtShadowColor);
        }
    }

    private static void DrawEmissiveKeypadAccents(
        ShadowLightingController controller,
        IReadOnlyList<Vector2> keypadPositions,
        float elapsedSeconds)
    {
        if (keypadPositions.Count == 0)
        {
            return;
        }

        float pulse = KeypadPulseBase + (KeypadPulseAmplitude * MathF.Sin(elapsedSeconds * KeypadPulseSpeed));
        bool alarmActive = controller.EmergencyPulseIntensity > 0f;

        Color outerColor = WithAlpha(
            alarmActive ? KeypadAlarmOuterBaseColor : KeypadNormalOuterBaseColor,
            (byte)(KeypadOuterBaseAlpha * pulse));
        Color innerColor = WithAlpha(
            alarmActive ? KeypadAlarmInnerBaseColor : KeypadNormalInnerBaseColor,
            (byte)(KeypadInnerBaseAlpha * pulse));
        Color beaconColor = WithAlpha(
            alarmActive ? KeypadAlarmBeaconBaseColor : KeypadNormalBeaconBaseColor,
            (byte)(KeypadBeaconBaseAlpha + (KeypadBeaconPulseAlpha * pulse)));

        foreach (Vector2 keypadPosition in keypadPositions)
        {
            var floorPoolCenter = new Vector3(keypadPosition.X, KeypadGlowElevationY, keypadPosition.Y);
            Raylib.DrawCylinder(floorPoolCenter, KeypadPoolOuterRadius, KeypadPoolOuterRadius, KeypadPoolHeight, KeypadPoolSlices, outerColor);
            Raylib.DrawCylinder(floorPoolCenter, KeypadPoolInnerRadius, KeypadPoolInnerRadius, KeypadPoolHeight, KeypadPoolSlices, innerColor);
            Raylib.DrawSphere(new Vector3(keypadPosition.X, KeypadWallBeaconY, keypadPosition.Y), KeypadBeaconRadius, beaconColor);
        }
    }

    private static Color WithAlpha(Color color, byte alpha) => new(color.R, color.G, color.B, alpha);
}
