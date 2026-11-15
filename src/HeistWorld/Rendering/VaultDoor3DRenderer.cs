using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.Rendering;

/// <summary>
/// Renders 3D vault door frames, per-door hinged leaf animations, security seals, and status beacons.
/// </summary>
public sealed class VaultDoor3DRenderer
{
    private const float FrameWidth = 92f;
    private const float FrameHeight = 88f;
    private const float PostDepth = 32f;
    private const float PostThickness = 12f;
    private const float HeaderDepth = 34f;
    private const float HeaderHeight = 14f;
    private const float HeaderPadding = 14f;
    private const float DoorOpenSpeedDegrees = 160f;
    private const float DoorOpenAngleDegrees = 92f;
    private const float DoorLeafThickness = 16f;
    private const float DoorLeafLength = 80f;
    private const float DoorLeafHalfSpan = DoorLeafLength * 0.5f;
    private const float WheelRadius = 13f;
    private const int WheelSides = 12;
    private const int SecurityBeamCount = 3;
    private const float SecurityBeamBaseHeight = 24f;
    private const float SecurityBeamStep = 18f;

    private static readonly Color SteelColor = new(138, 152, 172, 255);
    private static readonly Color UnlockedSteelColor = new(172, 198, 212, 255);
    private static readonly Color DarkSteelColor = new(54, 64, 80, 255);
    private static readonly Color BrassColor = new(212, 162, 68, 255);
    private static readonly Color CyanGlowColor = new(56, 225, 255, 255);
    private static readonly Color EmeraldGlowColor = new(74, 222, 128, 255);
    private static readonly Color RedAlertColor = new(244, 63, 94, 255);

    private readonly Dictionary<string, float> _swingAngles = new();

    public void Reset() => _swingAngles.Clear();

    public void Update(float deltaTime, GameContext context)
    {
        float maxStep = DoorOpenSpeedDegrees * deltaTime;
        foreach (VaultDoor door in context.Interactables.OfType<VaultDoor>())
        {
            float currentAngle = _swingAngles.GetValueOrDefault(door.Id);
            float targetAngle = door.IsOpen ? DoorOpenAngleDegrees : 0f;
            float delta = targetAngle - currentAngle;

            _swingAngles[door.Id] = MathF.Abs(delta) <= maxStep
                ? targetAngle
                : currentAngle + (MathF.Sign(delta) * maxStep);
        }
    }

    public void Draw(VaultDoor door, float timeSeconds)
    {
        var frameCenter = new Vector3(door.Position.X, 0f, door.Position.Y);
        DrawFrame(frameCenter);
        DrawHingedDoorLeaf(frameCenter, door.IsLocked, _swingAngles.GetValueOrDefault(door.Id));
        DrawStatusBeacon(frameCenter, door, timeSeconds);

        if (door.IsLocked)
        {
            DrawSecuritySeal(door.Position, timeSeconds);
        }
    }

    private static void DrawFrame(Vector3 center)
    {
        float postCenterY = FrameHeight * 0.5f;
        float halfFrameWidth = FrameWidth * 0.5f;

        Raylib.DrawCube(center + new Vector3(0f, postCenterY, -halfFrameWidth), PostDepth, FrameHeight, PostThickness, DarkSteelColor);
        Raylib.DrawCube(center + new Vector3(0f, postCenterY, halfFrameWidth), PostDepth, FrameHeight, PostThickness, DarkSteelColor);
        Raylib.DrawCube(center + new Vector3(0f, FrameHeight + 5f, 0f), HeaderDepth, HeaderHeight, FrameWidth + HeaderPadding, SteelColor);
    }

    private static void DrawHingedDoorLeaf(Vector3 frameCenter, bool isLocked, float swingDegrees)
    {
        float leafCenterY = FrameHeight * 0.48f;
        float leafHeight = FrameHeight - 6f;
        Color doorTint = isLocked ? SteelColor : UnlockedSteelColor;

        Rlgl.PushMatrix();
        Rlgl.Translatef(frameCenter.X, leafCenterY, frameCenter.Z - DoorLeafHalfSpan);
        Rlgl.Rotatef(swingDegrees, 0f, 1f, 0f);

        var localLeafCenter = new Vector3(0f, 0f, DoorLeafHalfSpan);
        Raylib3DPrimitives.DrawOutlinedCube(localLeafCenter, new Vector3(DoorLeafThickness, leafHeight, DoorLeafLength), doorTint, Viewport3DConfig.OutlineColor);

        var wheelStart = new Vector3(7f, 0f, DoorLeafHalfSpan);
        var wheelEnd = new Vector3(12f, 0f, DoorLeafHalfSpan);
        Raylib.DrawCylinderEx(wheelStart, wheelEnd, WheelRadius, WheelRadius, WheelSides, BrassColor);

        Rlgl.PopMatrix();
    }

    private static void DrawStatusBeacon(Vector3 center, VaultDoor door, float timeSeconds)
    {
        Color statusColor = door.IsOpen ? EmeraldGlowColor : (door.IsLocked ? RedAlertColor : CyanGlowColor);
        byte alpha = (byte)(255f * (0.6f + (0.4f * MathF.Sin(timeSeconds * 5f))));
        Raylib.DrawSphere(
            center + new Vector3(12f, FrameHeight + 16f, 0f),
            6f,
            new Color(statusColor.R, statusColor.G, statusColor.B, alpha));
    }

    private static void DrawSecuritySeal(Vector2 doorPosition, float timeSeconds)
    {
        byte glowAlpha = (byte)(95f + (50f * MathF.Sin(timeSeconds * 4.5f)));
        byte coreAlpha = (byte)(190f + (60f * MathF.Sin(timeSeconds * 4.5f)));
        var glow = new Color(RedAlertColor.R, RedAlertColor.G, RedAlertColor.B, glowAlpha);
        var core = new Color((byte)255, (byte)130, (byte)150, coreAlpha);

        for (int i = 0; i < SecurityBeamCount; i++)
        {
            float beamHeight = SecurityBeamBaseHeight + (i * SecurityBeamStep);
            var start = new Vector3(doorPosition.X - 16f, beamHeight, doorPosition.Y - DoorLeafHalfSpan);
            var end = new Vector3(doorPosition.X - 16f, beamHeight, doorPosition.Y + DoorLeafHalfSpan);
            Raylib3DPrimitives.DrawVolumetricBeam(start, end, glowRadius: 1.4f, coreRadius: 0.45f, glow, core);
        }
    }
}
