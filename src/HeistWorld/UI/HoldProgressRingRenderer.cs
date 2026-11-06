using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.HeistWorld.UI;

public static class HoldProgressRingRenderer
{
    private const float InnerRadius = 22f;
    private const float OuterRadius = 26f;
    private const float StartAngleDegrees = -90f;
    private const int CircleSegments = 36;

    private static readonly Color TrackColor = new(255, 255, 255, 45);
    private static readonly Color FillColor = new(56, 189, 248, 220);
    private static readonly Color OutlineColor = new(56, 189, 248, 80);

    public static void Draw(Vector2 center, float progress)
    {
        if (progress <= 0f)
        {
            return;
        }

        Raylib.DrawRing(center, InnerRadius, OuterRadius, 0f, 360f, CircleSegments, TrackColor);

        float clamped = Math.Clamp(progress, 0f, 1f);
        float endAngle = StartAngleDegrees + (clamped * 360f);
        Raylib.DrawRing(center, InnerRadius, OuterRadius, StartAngleDegrees, endAngle, CircleSegments, FillColor);

        Raylib.DrawRingLines(center, InnerRadius, OuterRadius, 0f, 360f, CircleSegments, OutlineColor);
    }
}
