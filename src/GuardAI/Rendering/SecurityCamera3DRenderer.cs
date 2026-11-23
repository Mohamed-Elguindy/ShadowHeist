using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.WorldLighting.Rendering;

namespace ShadowHeist.GuardAI.Rendering;

/// <summary>
/// Renders wall-mounted 3D SecurityCamera housings, sweeping floor-projected VisionRaycaster cones, and detection gauges (Ticket #29 — Hatem).
/// </summary>
public static class SecurityCamera3DRenderer
{
    private const float MountElevationY = 82f;
    private const float BracketSize = 8f;
    private const float BarrelLength = 16f;
    private const float LensTipDistance = 17f;
    private const float BarrelStartRadius = 4.5f;
    private const float BarrelEndRadius = 3.5f;
    private const int BarrelSides = 8;
    private const float DownwardPitchSlope = -0.36f;
    private const float LedSphereRadius = 2.4f;
    private const float LedBlinkSpeed = 4f;
    private const float LedMinAlpha = 120f;
    private const float LedAlphaPulseRange = 135f;
    private const byte LedRedByte = 255;
    private const byte LedGreenByte = 55;
    private const byte LedBlueByte = 75;
    private const float VisionConeGroundY = 1.8f;
    private const float GaugeElevationOffsetY = 14f;
    private const float GaugeFullWidth = 20f;
    private const float MinGaugeThreshold = 0.02f;

    private static readonly Color BracketDarkSteel = new(44, 52, 66, 255);
    private static readonly Color HousingLightSteel = new(210, 220, 234, 255);
    private static readonly Color HousingOfflineColor = new(82, 92, 108, 255);
    private static readonly Color LedOfflineColor = new(64, 72, 86, 200);
    private static readonly Color ConeIdleFill = new(56, 225, 255, 76);
    private static readonly Color ConeIdleEdge = new(125, 240, 255, 185);
    private static readonly Color ConeDetectingFill = new(251, 146, 60, 110);
    private static readonly Color ConeDetectingEdge = new(253, 186, 116, 215);
    private static readonly Color ConeAlertFill = new(244, 63, 94, 120);
    private static readonly Color ConeAlertEdge = new(255, 110, 125, 225);

    public static void Draw(
        IReadOnlyList<SecurityCamera> cameras,
        IReadOnlyList<RectangleBounds> visionBlockers,
        bool isAlarmTriggered,
        float elapsedSeconds)
    {
        foreach (SecurityCamera camera in cameras)
        {
            DrawCameraHousing(camera, elapsedSeconds);

            if (!camera.IsActive)
            {
                continue;
            }

            DrawCameraVisionCone(camera, visionBlockers, isAlarmTriggered);
            if (camera.DetectionProgress > MinGaugeThreshold)
            {
                DrawCameraDetectionGauge(camera);
            }
        }
    }

    private static void DrawCameraHousing(SecurityCamera camera, float timeSeconds)
    {
        var mountPos = new Vector3(camera.Position.X, MountElevationY, camera.Position.Y);
        Raylib.DrawCube(mountPos, BracketSize, BracketSize, BracketSize, BracketDarkSteel);

        float radians = camera.CurrentAngleDegrees * HeistConstants.DegToRad;
        Vector3 lookDir = new(MathF.Cos(radians), DownwardPitchSlope, MathF.Sin(radians));
        Vector3 barrelEnd = mountPos + (lookDir * BarrelLength);
        Vector3 lensTip = mountPos + (lookDir * LensTipDistance);

        Color bodyColor = camera.IsActive ? HousingLightSteel : HousingOfflineColor;
        Raylib.DrawCylinderEx(mountPos, barrelEnd, BarrelStartRadius, BarrelEndRadius, BarrelSides, bodyColor);

        Color ledColor = ResolveLedColor(camera.IsActive, timeSeconds);
        Raylib.DrawSphere(lensTip, LedSphereRadius, ledColor);
    }

    private static Color ResolveLedColor(bool isActive, float timeSeconds)
    {
        if (!isActive)
        {
            return LedOfflineColor;
        }

        byte alpha = (byte)(LedMinAlpha + (LedAlphaPulseRange * MathF.Abs(MathF.Sin(timeSeconds * LedBlinkSpeed))));
        return new Color(LedRedByte, LedGreenByte, LedBlueByte, alpha);
    }

    private static void DrawCameraVisionCone(
        SecurityCamera camera,
        IReadOnlyList<RectangleBounds> visionBlockers,
        bool isAlarmTriggered)
    {
        IReadOnlyList<Vector2> cone = camera.ComputeVisionCone(visionBlockers);
        if (cone.Count < 3)
        {
            return;
        }

        (Color fill, Color edge) = ResolveConeColors(camera.DetectionProgress, isAlarmTriggered);
        VisionCone3DRenderer.DrawProjectedVisionPolygon(cone, VisionConeGroundY, MountElevationY, fill, edge);
    }

    private static (Color Fill, Color Edge) ResolveConeColors(float progress, bool isAlarmTriggered)
    {
        if (isAlarmTriggered || progress >= 1f)
        {
            return (ConeAlertFill, ConeAlertEdge);
        }

        if (progress > MinGaugeThreshold)
        {
            return (ConeDetectingFill, ConeDetectingEdge);
        }

        return (ConeIdleFill, ConeIdleEdge);
    }

    private static void DrawCameraDetectionGauge(SecurityCamera camera)
    {
        var barCenter = new Vector3(camera.Position.X, MountElevationY + GaugeElevationOffsetY, camera.Position.Y);
        Color fillColor = camera.DetectionProgress >= 1f ? ConeAlertEdge : ConeDetectingEdge;
        GuardDetectionBadge3DRenderer.DrawDetectionMeterBar(barCenter, camera.DetectionProgress, fillColor, GaugeFullWidth);
    }
}
