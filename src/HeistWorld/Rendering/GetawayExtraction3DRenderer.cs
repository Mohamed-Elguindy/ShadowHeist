using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.HeistWorld.Rendering;

/// <summary>
/// Renders 3D extraction beacons, corner bollards, and floating markers over the GetawayVanZone.
/// </summary>
public static class GetawayExtraction3DRenderer
{
    private const float BollardSize = 6f;
    private const float BollardHeight = 14f;
    private const float BeaconHeight = 96f;
    private const float BeaconRadius = 6f;
    private const float MarkerBaseHeight = 76f;
    private const float MarkerBobAmplitude = 8f;
    private const float MarkerBobFrequency = 3.5f;
    private const float MarkerSize = 14f;
    private const float PulseFrequency = 4f;
    private const float GroundRingY = 2.2f;
    private const float GroundRingMaxRadius = 52f;
    private const float GroundRingCycleSeconds = 1.6f;
    private const float FlatGroundRotationDegrees = 90f;
    private const int BeaconSegments = 12;

    private static readonly Color LockedAccentColor = new(56, 189, 248, 200);
    private static readonly Color ReadyAccentColor = new(74, 222, 128, 255);
    private static readonly Color BeaconCoreColor = new(134, 239, 172, 95);

    public static void Draw(RectangleBounds getawayZone, GameContext context)
    {
        if (context.Outcome != HeistOutcome.Playing)
        {
            return;
        }

        bool isReady = context.StolenCash >= HeistConstants.TargetCashGoal;
        Color accent = isReady ? ReadyAccentColor : LockedAccentColor;

        DrawCornerBollards(getawayZone, accent);
        if (isReady)
        {
            DrawReadyExtractionBeacon(getawayZone, context.ElapsedSeconds);
        }
    }

    private static void DrawCornerBollards(RectangleBounds zone, Color accentColor)
    {
        float halfY = BollardHeight * 0.5f;
        var size = new Vector3(BollardSize, BollardHeight, BollardSize);

        DrawBollard(new Vector3(zone.X, halfY, zone.Y), size, accentColor);
        DrawBollard(new Vector3(zone.X + zone.Width, halfY, zone.Y), size, accentColor);
        DrawBollard(new Vector3(zone.X, halfY, zone.Y + zone.Height), size, accentColor);
        DrawBollard(new Vector3(zone.X + zone.Width, halfY, zone.Y + zone.Height), size, accentColor);
    }

    private static void DrawBollard(Vector3 position, Vector3 size, Color accentColor) =>
        Raylib3DPrimitives.DrawOutlinedCube(position, size, accentColor, Viewport3DConfig.OutlineColor);

    private static void DrawReadyExtractionBeacon(RectangleBounds zone, float timeSeconds)
    {
        Vector2 center = zone.Center;
        byte pulseAlpha = (byte)(50f + (45f * MathF.Sin(timeSeconds * PulseFrequency)));
        var beamColor = new Color(BeaconCoreColor.R, BeaconCoreColor.G, BeaconCoreColor.B, pulseAlpha);
        var beamCenter = new Vector3(center.X, BeaconHeight * 0.5f, center.Y);

        Raylib.DrawCylinder(beamCenter, BeaconRadius, BeaconRadius, BeaconHeight, BeaconSegments, beamColor);
        DrawExpandingGroundRing(center, timeSeconds);
        DrawFloatingMarker(center, timeSeconds);
    }

    private static void DrawExpandingGroundRing(Vector2 center, float timeSeconds)
    {
        float cycle = (timeSeconds % GroundRingCycleSeconds) / GroundRingCycleSeconds;
        float radius = MathF.Max(BeaconRadius, cycle * GroundRingMaxRadius);
        byte alpha = (byte)(180f * (1f - cycle));
        var ringColor = new Color(ReadyAccentColor.R, ReadyAccentColor.G, ReadyAccentColor.B, alpha);
        Raylib.DrawCircle3D(new Vector3(center.X, GroundRingY, center.Y), radius, Vector3.UnitX, FlatGroundRotationDegrees, ringColor);
    }

    private static void DrawFloatingMarker(Vector2 zoneCenter, float timeSeconds)
    {
        float bobOffset = MathF.Sin(timeSeconds * MarkerBobFrequency) * MarkerBobAmplitude;
        var markerPosition = new Vector3(zoneCenter.X, MarkerBaseHeight + bobOffset, zoneCenter.Y);
        Raylib.DrawCube(markerPosition, MarkerSize, MarkerSize, MarkerSize, ReadyAccentColor);
        Raylib.DrawCubeWires(markerPosition, MarkerSize + 2f, MarkerSize + 2f, MarkerSize + 2f, Viewport3DConfig.OutlineColor);
    }
}
