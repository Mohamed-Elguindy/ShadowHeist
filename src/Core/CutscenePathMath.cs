using System.Numerics;

namespace ShadowHeist.Core;

/// <summary>
/// Pure interpolation curves and van/player pose math for entry and escape cutscenes.
/// </summary>
internal static class CutscenePathMath
{
    public const float EntryDriveDurationSeconds = 1.35f;
    public const float EntryWalkDurationSeconds = 0.90f;
    public const float EntryTotalDurationSeconds = EntryDriveDurationSeconds + EntryWalkDurationSeconds;

    public const float EscapeBoardDurationSeconds = 0.75f;
    public const float EscapeDriveDurationSeconds = 1.45f;
    public const float EscapeSummaryDelaySeconds = EscapeBoardDurationSeconds + EscapeDriveDurationSeconds;
    public const float EscapeTotalDurationSeconds = EscapeBoardDurationSeconds + EscapeDriveDurationSeconds;

    private const float VanTurnInStartFraction = 0.60f;
    private const float VanDoorOffsetY = -54f;
    private const float ParkedVanYawDegrees = 14f;
    private const float RoadVanYawDegrees = 0f;
    private const float MinFacingDistanceSq = 0.01f;
    private const float HalfProgress = 0.5f;

    public static readonly Vector2 ParkedVanPosition = new(134f, 828f);
    public static readonly Vector2 VanDoorPosition = ParkedVanPosition + new Vector2(0f, VanDoorOffsetY);
    private static readonly Vector2 EntryVanStartPosition = new(-240f, 846f);
    private static readonly Vector2 EscapeVanEndPosition = new(840f, 856f);

    public static Vector2 EvaluateEntryVanPosition(float elapsedSeconds)
    {
        float t = Math.Clamp(elapsedSeconds / EntryDriveDurationSeconds, 0f, 1f);
        return Vector2.Lerp(EntryVanStartPosition, ParkedVanPosition, EaseOutCubic(t));
    }

    public static float EvaluateEntryVanYaw(float elapsedSeconds)
    {
        float driveT = Math.Clamp(elapsedSeconds / EntryDriveDurationSeconds, 0f, 1f);
        float turnT = Math.Clamp((driveT - VanTurnInStartFraction) / (1f - VanTurnInStartFraction), 0f, 1f);
        return MathF.FusedMultiplyAdd(ParkedVanYawDegrees - RoadVanYawDegrees, EaseOutCubic(turnT), RoadVanYawDegrees);
    }

    public static Vector2 EvaluateEscapeVanPosition(float elapsedSeconds)
    {
        if (elapsedSeconds <= EscapeBoardDurationSeconds)
        {
            return ParkedVanPosition;
        }

        float driveT = Math.Clamp((elapsedSeconds - EscapeBoardDurationSeconds) / EscapeDriveDurationSeconds, 0f, 1f);
        return Vector2.Lerp(ParkedVanPosition, EscapeVanEndPosition, EaseInCubic(driveT));
    }

    public static float EvaluateEscapeVanYaw(float elapsedSeconds)
    {
        if (elapsedSeconds <= EscapeBoardDurationSeconds)
        {
            return ParkedVanYawDegrees;
        }

        float driveT = Math.Clamp((elapsedSeconds - EscapeBoardDurationSeconds) / (EscapeDriveDurationSeconds * HalfProgress), 0f, 1f);
        return MathF.FusedMultiplyAdd(RoadVanYawDegrees - ParkedVanYawDegrees, EaseOutCubic(driveT), ParkedVanYawDegrees);
    }

    public static Vector2 EvaluateEntryPlayerPosition(float elapsedSeconds, Vector2 spawnPosition)
    {
        if (elapsedSeconds < EntryDriveDurationSeconds)
        {
            return VanDoorPosition;
        }

        float walkT = Math.Clamp((elapsedSeconds - EntryDriveDurationSeconds) / EntryWalkDurationSeconds, 0f, 1f);
        return Vector2.Lerp(VanDoorPosition, spawnPosition, EaseOutCubic(walkT));
    }

    public static Vector2 EvaluateEscapePlayerPosition(float elapsedSeconds, Vector2 startPosition)
    {
        float boardT = Math.Clamp(elapsedSeconds / EscapeBoardDurationSeconds, 0f, 1f);
        return Vector2.Lerp(startPosition, VanDoorPosition, EaseInOutQuad(boardT));
    }

    public static float ComputeFacingDegrees(Vector2 from, Vector2 to, float fallbackDegrees)
    {
        Vector2 delta = to - from;
        return delta.LengthSquared() > MinFacingDistanceSq ? MathF.Atan2(delta.Y, delta.X) * HeistConstants.RadToDeg : fallbackDegrees;
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - (inv * inv * inv);
    }

    private static float EaseInCubic(float t) => t * t * t;

    private static float EaseInOutQuad(float t) =>
        t < HalfProgress ? 2f * t * t : 1f - (MathF.Pow((-2f * t) + 2f, 2f) * HalfProgress);
}
