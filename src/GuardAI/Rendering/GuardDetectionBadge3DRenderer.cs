using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.GuardAI.Rendering;

/// <summary>
/// Renders 3D close-proximity awareness circles and overhead progressive ?/! detection gauges for guards (Ticket #22 — Hatem).
/// </summary>
public static class GuardDetectionBadge3DRenderer
{
    private const float ProximityRingGroundY = 1.4f;
    private const float ProximityRingHeight = 0.4f;
    private const int ProximityRingSides = 28;
    private const float BadgeBaseElevationY = 84f;
    private const float BadgeAlertBobSpeed = 10f;
    private const float BadgeIdleBobSpeed = 3f;
    private const float BadgeBobAmplitude = 2.5f;
    private const float BadgeAlertConeHeight = 13f;
    private const float BadgeIdleConeHeight = 6f;
    private const float BadgeAlertConeRadius = 4.6f;
    private const float BadgeIdleConeRadius = 3.2f;
    private const float BadgeConeTipRadius = 0.5f;
    private const int BadgeConeSides = 6;
    private const float BadgeDotOffsetY = -4f;
    private const float BadgeDotRadius = 2.5f;
    private const float GaugeOffsetY = 16f;
    private const float GaugeFullWidth = 22f;
    private const float GaugeHeight = 3.2f;
    private const float GaugeDepth = 3.2f;
    private const float MinGaugeDisplayThreshold = 0.02f;

    private static readonly Color ProximityIdleColor = new(255, 214, 78, 85);
    private static readonly Color ProximityDetectingColor = new(251, 146, 60, 165);
    private static readonly Color ProximityAlertColor = new(244, 63, 94, 195);
    private static readonly Color AlertBadgeColor = new(244, 63, 94, 245);
    private static readonly Color SuspiciousBadgeColor = new(250, 204, 21, 240);
    private static readonly Color PatrolBadgeColor = new(255, 214, 78, 175);
    private static readonly Color GaugeTrackColor = new(18, 24, 38, 220);

    public static void DrawProximityCircle(Guard guard, bool alarmActive)
    {
        bool isAlert = alarmActive || guard.CurrentState.StateName == "Alert" || guard.DetectionProgress >= 1f;
        Color ringColor = isAlert
            ? ProximityAlertColor
            : guard.DetectionProgress > MinGaugeDisplayThreshold ? ProximityDetectingColor : ProximityIdleColor;

        var ringCenter = new Vector3(guard.Position.X, ProximityRingGroundY, guard.Position.Y);
        Raylib.DrawCylinderWires(
            ringCenter,
            GuardSensorHelper.ProximityAwarenessRadius,
            GuardSensorHelper.ProximityAwarenessRadius,
            ProximityRingHeight,
            ProximityRingSides,
            ringColor);
    }

    public static void DrawAwarenessBadgeAndGauge(Guard guard, float timeSeconds)
    {
        string stateName = guard.CurrentState.StateName;
        float progress = guard.DetectionProgress;
        bool isAlert = stateName == "Alert" || progress >= 1f;
        bool isSuspicious = stateName == "Suspicious" || progress > MinGaugeDisplayThreshold;

        float bobSpeed = isAlert ? BadgeAlertBobSpeed : BadgeIdleBobSpeed;
        float bobY = BadgeBaseElevationY + (MathF.Sin(timeSeconds * bobSpeed) * BadgeBobAmplitude);
        var badgeBase = new Vector3(guard.Position.X, bobY, guard.Position.Y);
        Color badgeColor = ResolveBadgeColor(isAlert, isSuspicious, progress);

        float coneHeight = BadgeIdleConeHeight + ((BadgeAlertConeHeight - BadgeIdleConeHeight) * (isAlert ? 1f : progress));
        float coneRadius = BadgeIdleConeRadius + ((BadgeAlertConeRadius - BadgeIdleConeRadius) * (isAlert ? 1f : progress));
        Raylib.DrawCylinderEx(badgeBase, badgeBase + new Vector3(0f, coneHeight, 0f), BadgeConeTipRadius, coneRadius, BadgeConeSides, badgeColor);

        if (isAlert || isSuspicious)
        {
            Raylib.DrawSphere(badgeBase + new Vector3(0f, BadgeDotOffsetY, 0f), BadgeDotRadius, badgeColor);
        }

        if (progress > MinGaugeDisplayThreshold && progress < 1f)
        {
            DrawDetectionMeterBar(badgeBase + new Vector3(0f, GaugeOffsetY, 0f), progress, badgeColor);
        }
    }

    private static Color ResolveBadgeColor(bool isAlert, bool isSuspicious, float progress)
    {
        if (isAlert)
        {
            return AlertBadgeColor;
        }

        if (progress > MinGaugeDisplayThreshold)
        {
            byte green = (byte)(204f - (141f * progress));
            return new Color((byte)250, green, (byte)45, (byte)245);
        }

        return isSuspicious ? SuspiciousBadgeColor : PatrolBadgeColor;
    }

    private static void DrawDetectionMeterBar(Vector3 barCenter, float progress, Color fillColor)
    {
        float clamped = Math.Clamp(progress, 0f, 1f);
        float fillWidth = GaugeFullWidth * clamped;
        float leftEdgeX = barCenter.X - (GaugeFullWidth * 0.5f);
        var fillCenter = new Vector3(leftEdgeX + (fillWidth * 0.5f), barCenter.Y, barCenter.Z);

        Raylib.DrawCube(barCenter, GaugeFullWidth, GaugeHeight, GaugeDepth, GaugeTrackColor);
        Raylib.DrawCube(fillCenter, fillWidth, GaugeHeight + 0.4f, GaugeDepth + 0.4f, fillColor);
    }
}
