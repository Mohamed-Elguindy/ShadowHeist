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

    private static readonly Vector2 DefaultKeypadPosition = new(988f, 368f);
    private static readonly Color WallSkirtShadowColor = new(6, 9, 16, 64);

    public void Draw(
        ShadowLightingController lightingController,
        IReadOnlyList<RectangleBounds> architecturalWalls,
        float elapsedSeconds)
    {
        Rlgl.DisableDepthMask();
        DrawZoneDarknessVeils(lightingController);
        DrawWallAmbientShadowSkirts(architecturalWalls);
        DrawEmissiveKeypadAccents(lightingController, elapsedSeconds);
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
            new Color((byte)6, (byte)10, (byte)20, floorAlpha));

        Vector2 vaultCenter2D = (Viewport3DConfig.VaultZoneMin + Viewport3DConfig.VaultZoneMax) * HalfScale;
        Vector2 vaultSize2D = Viewport3DConfig.VaultZoneMax - Viewport3DConfig.VaultZoneMin;
        float vaultDarkness = controller.GetZoneDarkness(vaultCenter2D);
        byte vaultAlpha = (byte)Math.Clamp(vaultDarkness * MaxVaultFloorAlpha, 0f, MaxVaultFloorAlpha);

        Raylib.DrawCube(
            new Vector3(vaultCenter2D.X, VaultFloorVeilElevationY, vaultCenter2D.Y),
            vaultSize2D.X,
            FloorShadowVeilHeight,
            vaultSize2D.Y,
            new Color((byte)8, (byte)12, (byte)24, vaultAlpha));

        if (controller.EmergencyPulseIntensity > 0f)
        {
            byte emergencyAlpha = (byte)Math.Clamp(controller.EmergencyPulseIntensity * MaxEmergencyVeilAlpha, 0f, MaxEmergencyVeilAlpha);
            Raylib.DrawCube(
                floorCenter,
                Viewport3DConfig.MapWidth,
                FloorShadowVeilHeight,
                Viewport3DConfig.MapHeight,
                new Color((byte)220, (byte)38, (byte)68, emergencyAlpha));
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

    private static void DrawEmissiveKeypadAccents(ShadowLightingController controller, float elapsedSeconds)
    {
        float pulse = KeypadPulseBase + (KeypadPulseAmplitude * MathF.Sin(elapsedSeconds * KeypadPulseSpeed));
        bool alarmActive = controller.EmergencyPulseIntensity > 0f;

        byte outerAlpha = (byte)(KeypadOuterBaseAlpha * pulse);
        byte innerAlpha = (byte)(KeypadInnerBaseAlpha * pulse);
        byte beaconAlpha = (byte)(KeypadBeaconBaseAlpha + (KeypadBeaconPulseAlpha * pulse));

        Color outerColor = alarmActive
            ? new Color((byte)244, (byte)63, (byte)94, outerAlpha)
            : new Color((byte)56, (byte)225, (byte)255, outerAlpha);
        Color innerColor = alarmActive
            ? new Color((byte)255, (byte)96, (byte)116, innerAlpha)
            : new Color((byte)74, (byte)222, (byte)128, innerAlpha);
        Color beaconColor = alarmActive
            ? new Color((byte)255, (byte)80, (byte)100, beaconAlpha)
            : new Color((byte)110, (byte)242, (byte)185, beaconAlpha);

        var floorPoolCenter = new Vector3(DefaultKeypadPosition.X, KeypadGlowElevationY, DefaultKeypadPosition.Y);
        Raylib.DrawCylinder(floorPoolCenter, KeypadPoolOuterRadius, KeypadPoolOuterRadius, KeypadPoolHeight, KeypadPoolSlices, outerColor);
        Raylib.DrawCylinder(floorPoolCenter, KeypadPoolInnerRadius, KeypadPoolInnerRadius, KeypadPoolHeight, KeypadPoolSlices, innerColor);
        Raylib.DrawSphere(new Vector3(DefaultKeypadPosition.X, KeypadWallBeaconY, DefaultKeypadPosition.Y), KeypadBeaconRadius, beaconColor);
    }
}
