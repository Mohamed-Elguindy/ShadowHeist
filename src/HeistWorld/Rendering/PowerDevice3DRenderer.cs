using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.Rendering;

/// <summary>
/// Renders 3D FuseBox cabinets and LaserSwitch pedestals with animated levers and status beacons.
/// </summary>
public static class PowerDevice3DRenderer
{
    private const float DeviceShadowRadius = 14f;
    private const float BeaconRadius = 3.4f;
    private const float BeaconHeightOffset = 4.5f;
    private const float PoweredPulseFrequency = 4.5f;
    private const float CutPulseFrequency = 0f;
    private const float LeverPoweredAngleDegrees = -28f;
    private const float LeverDisabledAngleDegrees = 28f;

    private static readonly Vector3 FuseBoxSize = new(20f, 44f, 16f);
    private static readonly Vector3 FusePanelSize = new(12f, 16f, 2f);
    private static readonly Vector3 SwitchPedestalSize = new(16f, 38f, 16f);
    private static readonly Vector3 SwitchPanelSize = new(10f, 10f, 2f);
    private static readonly Vector3 LeverArmSize = new(3.5f, 13f, 3.5f);

    private static readonly Color FuseChassisColor = new(44, 52, 66, 255);
    private static readonly Color SwitchChassisColor = new(36, 44, 58, 255);
    private static readonly Color LeverMetalColor = new(186, 198, 214, 255);
    private static readonly Color AmberPoweredColor = new(248, 196, 44, 255);
    private static readonly Color SlateOfflineColor = new(90, 98, 114, 255);
    private static readonly Color CrimsonArmedColor = new(244, 63, 94, 255);
    private static readonly Color EmeraldSafeColor = new(74, 222, 128, 255);

    public static void DrawFuseBox(FuseBox fuseBox, float timeSeconds)
    {
        Color statusColor = ResolveFuseBoxColor(fuseBox.IsPowered);
        float pulseFrequency = fuseBox.IsPowered ? PoweredPulseFrequency : CutPulseFrequency;

        Raylib3DPrimitives.DrawGroundShadow(fuseBox.Position, DeviceShadowRadius);
        DrawCabinet(fuseBox.Position, FuseBoxSize, FuseChassisColor);
        DrawStatusPanel(fuseBox.Position, FuseBoxSize.Y * 0.62f, FuseBoxSize.Z, FusePanelSize, statusColor);
        DrawBreakerLever(fuseBox.Position, FuseBoxSize.Y * 0.42f, FuseBoxSize.Z * 0.5f, fuseBox.IsPowered);
        DrawStatusBeacon(fuseBox.Position, FuseBoxSize.Y, statusColor, pulseFrequency, timeSeconds);
    }

    public static void DrawLaserSwitch(LaserSwitch laserSwitch, float timeSeconds)
    {
        Color statusColor = ResolveSwitchColor(laserSwitch.IsPowered);

        Raylib3DPrimitives.DrawGroundShadow(laserSwitch.Position, DeviceShadowRadius);
        DrawCabinet(laserSwitch.Position, SwitchPedestalSize, SwitchChassisColor);
        DrawStatusPanel(laserSwitch.Position, SwitchPedestalSize.Y * 0.55f, SwitchPedestalSize.Z, SwitchPanelSize, statusColor);
        DrawBreakerLever(laserSwitch.Position, SwitchPedestalSize.Y * 0.78f, SwitchPedestalSize.Z * 0.5f, laserSwitch.IsPowered);
        DrawStatusBeacon(laserSwitch.Position, SwitchPedestalSize.Y, statusColor, PoweredPulseFrequency, timeSeconds);
    }

    private static Color ResolveFuseBoxColor(bool isPowered) =>
        isPowered ? AmberPoweredColor : SlateOfflineColor;

    private static Color ResolveSwitchColor(bool isPowered) =>
        isPowered ? CrimsonArmedColor : EmeraldSafeColor;

    private static void DrawCabinet(Vector2 position, Vector3 size, Color chassisColor)
    {
        var center = new Vector3(position.X, size.Y * 0.5f, position.Y);
        Raylib3DPrimitives.DrawOutlinedCube(center, size, chassisColor, Viewport3DConfig.OutlineColor);
    }

    private static void DrawStatusPanel(
        Vector2 position,
        float elevationY,
        float cabinetDepth,
        Vector3 panelSize,
        Color statusColor)
    {
        var panelCenter = new Vector3(position.X, elevationY, position.Y + (cabinetDepth * 0.5f) + 0.8f);
        Raylib.DrawCube(panelCenter, panelSize.X, panelSize.Y, panelSize.Z, statusColor);
    }

    private static void DrawBreakerLever(
        Vector2 position,
        float pivotHeightY,
        float frontOffsetZ,
        bool isPowered)
    {
        float tiltAngle = isPowered ? LeverPoweredAngleDegrees : LeverDisabledAngleDegrees;

        Rlgl.PushMatrix();
        Rlgl.Translatef(position.X, pivotHeightY, position.Y + frontOffsetZ + 2f);
        Rlgl.Rotatef(tiltAngle, 1f, 0f, 0f);
        Raylib3DPrimitives.DrawOutlinedCube(
            new Vector3(0f, LeverArmSize.Y * 0.4f, 0f),
            LeverArmSize,
            LeverMetalColor,
            Viewport3DConfig.OutlineColor);
        Rlgl.PopMatrix();
    }

    private static void DrawStatusBeacon(
        Vector2 position,
        float topY,
        Color statusColor,
        float pulseFrequency,
        float timeSeconds)
    {
        byte alpha = ComputePulseAlpha(pulseFrequency, timeSeconds);
        var beaconColor = new Color(statusColor.R, statusColor.G, statusColor.B, alpha);
        Raylib.DrawSphere(new Vector3(position.X, topY + BeaconHeightOffset, position.Y), BeaconRadius, beaconColor);
    }

    private static byte ComputePulseAlpha(float pulseFrequency, float timeSeconds)
    {
        if (pulseFrequency <= 0f)
        {
            return 140;
        }

        return (byte)(155f + (100f * MathF.Sin(timeSeconds * pulseFrequency)));
    }
}
