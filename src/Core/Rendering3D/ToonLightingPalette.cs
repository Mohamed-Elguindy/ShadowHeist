using System.Numerics;

namespace ShadowHeist.Core.Rendering3D;

/// <summary>
/// Centralizes cel-shader lighting colors and helpers for normal, blackout, and alarm states.
/// </summary>
internal static class ToonLightingPalette
{
    public const float AlarmPulseSpeed = 8f;
    public const float ActorLightHeightY = 36f;
    public const float LampLightHeightY = 64f;

    public static readonly Vector3 DefaultLightDirection = Vector3.Normalize(new Vector3(-0.48f, -1f, -0.52f));
    public static readonly Vector3 DefaultLightColor = new(0.56f, 0.60f, 0.72f);
    public static readonly Vector3 BlackoutLightColor = new(0.16f, 0.20f, 0.29f);
    public static readonly Vector3 DefaultAmbient = new(0.24f, 0.28f, 0.39f);
    public static readonly Vector3 BlackoutAmbient = new(0.08f, 0.10f, 0.16f);
    public static readonly Vector3 DefaultRim = new(0.22f, 0.48f, 0.68f);
    public static readonly Vector3 AlarmAmbient = new(0.46f, 0.16f, 0.20f);
    public static readonly Vector3 AlarmRim = new(0.95f, 0.22f, 0.24f);
    public static readonly Vector4 InkOutlineColor = new(0.04f, 0.05f, 0.09f, 1f);
    public static readonly Vector3 OffstageLightPosition = new(-9999f, 0f, -9999f);

    public static float ComputeAlarmPulse(bool isAlarmTriggered, float elapsedSeconds) =>
        isAlarmTriggered ? 0.5f + (0.5f * MathF.Sin(elapsedSeconds * AlarmPulseSpeed)) : 0f;

    public static Vector3 ResolveLightColor(bool areLobbyLightsPowered) =>
        areLobbyLightsPowered ? DefaultLightColor : BlackoutLightColor;

    public static Vector3 ResolveAmbientColor(bool areLobbyLightsPowered, float alarmPulse)
    {
        Vector3 baseAmbient = areLobbyLightsPowered ? DefaultAmbient : BlackoutAmbient;
        return Vector3.Lerp(baseAmbient, AlarmAmbient, alarmPulse);
    }

    public static Vector3 ResolveRimColor(float alarmPulse) =>
        Vector3.Lerp(DefaultRim, AlarmRim, alarmPulse);

    public static Vector3 ResolveLampPosition(Vector2 lampPos, bool areLobbyLightsPowered) =>
        areLobbyLightsPowered
            ? new Vector3(lampPos.X, LampLightHeightY, lampPos.Y)
            : OffstageLightPosition;
}
