using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.HeistWorld.Rendering;

/// <summary>
/// Renders 3D hacking keypads, pulsing status LEDs, and active terminal data-link effects.
/// </summary>
public static class KeypadTerminal3DRenderer
{
    private const float ChassisSize = 18f;
    private const float ChassisHeight = 46f;
    private const float BarFullWidth = 34f;
    private const float HackBeamPlayerHeight = 40f;
    private const float PulseBaseRadius = 28f;
    private const float PulseAmplitude = 4f;
    private const float PulseFrequency = 8f;

    private static readonly Color ChassisColor = new(34, 42, 56, 255);
    private static readonly Color GoldColor = new(248, 196, 44, 255);
    private static readonly Color CyanGlowColor = new(56, 225, 255, 255);
    private static readonly Color EmeraldGlowColor = new(74, 222, 128, 255);

    public static void Draw(KeypadTerminal keypad, Vector2 playerPosition, float timeSeconds)
    {
        var terminalCenter = new Vector3(keypad.Position.X, ChassisHeight * 0.5f, keypad.Position.Y);
        Raylib3DPrimitives.DrawGroundShadow(keypad.Position, 15f);
        Raylib3DPrimitives.DrawOutlinedCube(
            terminalCenter,
            new Vector3(ChassisSize, ChassisHeight, ChassisSize),
            ChassisColor,
            Viewport3DConfig.OutlineColor);

        Color screenColor = keypad.IsHacked
            ? EmeraldGlowColor
            : (keypad.HoldProgress > 0f ? GoldColor : CyanGlowColor);
        Raylib.DrawCube(terminalCenter + new Vector3(0f, 12f, 9.5f), 12f, 14f, 2f, screenColor);

        if (keypad.HoldProgress > 0f && !keypad.IsHacked)
        {
            DrawActiveHackingEffect(keypad, terminalCenter, playerPosition, timeSeconds);
        }

        byte ledAlpha = (byte)(keypad.IsHacked ? 255f : 150f + (100f * MathF.Sin(timeSeconds * 4f)));
        Raylib.DrawSphere(
            terminalCenter + new Vector3(0f, (ChassisHeight * 0.5f) + 4f, 0f),
            3.5f,
            new Color(screenColor.R, screenColor.G, screenColor.B, ledAlpha));
    }

    private static void DrawActiveHackingEffect(
        KeypadTerminal keypad,
        Vector3 terminalCenter,
        Vector2 playerPosition,
        float timeSeconds)
    {
        Vector3 barOrigin = new(keypad.Position.X, ChassisHeight + 18f, keypad.Position.Y);
        float fillWidth = BarFullWidth * keypad.HoldProgress;

        Raylib.DrawCube(barOrigin, BarFullWidth + 4f, 7f, 4f, ChassisColor);
        Raylib.DrawCube(barOrigin + new Vector3((fillWidth - BarFullWidth) * 0.5f, 0f, 0.5f), fillWidth, 5f, 4f, GoldColor);
        Raylib.DrawLine3D(
            new Vector3(playerPosition.X, HackBeamPlayerHeight, playerPosition.Y),
            terminalCenter + new Vector3(0f, 12f, 0f),
            CyanGlowColor);

        float ringRadius = PulseBaseRadius + (MathF.Sin(timeSeconds * PulseFrequency) * PulseAmplitude);
        Raylib.DrawCylinderWires(new Vector3(keypad.Position.X, 2f, keypad.Position.Y), ringRadius, ringRadius, 1f, 18, CyanGlowColor);
    }
}
