using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.PlayerSystem.Rendering;

/// <summary>
/// Renders a subtle dashed tactical aim trace and pulsing ground target reticle at the player's aim point.
/// </summary>
public static class PlayerAim3DRenderer
{
    private const float GroundY = 2.6f;
    private const float MinAimDistance = 28f;
    private const float MaxTraceDistance = 260f;
    private const float DashLength = 14f;
    private const float DashStride = 26f;
    private const float ReticleInnerRadius = 7f;
    private const float ReticleOuterBaseRadius = 12f;
    private const float ReticlePulseAmplitude = 2.5f;
    private const float ReticlePulseSpeed = 5f;
    private const float FlatGroundRotationDegrees = 90f;

    private static readonly Color AimLineColor = new(56, 225, 255, 75);
    private static readonly Color ReticleOuterColor = new(56, 225, 255, 165);
    private static readonly Color ReticleInnerColor = new(224, 242, 254, 190);

    public static void Draw(GameContext context)
    {
        if (context.Outcome != HeistOutcome.Playing ||
            context.Cutscene.IsActive ||
            context.AimWorldOverride is not Vector2 aimWorld)
        {
            return;
        }

        Vector2 delta = aimWorld - context.PlayerPosition;
        float distance = delta.Length();
        if (distance < MinAimDistance)
        {
            return;
        }

        Vector2 direction = delta / distance;
        float traceLength = MathF.Min(distance, MaxTraceDistance);
        DrawDashedAimTrace(context.PlayerPosition, direction, traceLength);
        DrawGroundReticle(aimWorld, context.ElapsedSeconds);
    }

    private static void DrawDashedAimTrace(Vector2 origin, Vector2 direction, float traceLength)
    {
        for (float d = MinAimDistance; d < traceLength; d += DashStride)
        {
            float endD = MathF.Min(d + DashLength, traceLength);
            Vector2 p0 = origin + (direction * d);
            Vector2 p1 = origin + (direction * endD);
            Raylib.DrawLine3D(new Vector3(p0.X, GroundY, p0.Y), new Vector3(p1.X, GroundY, p1.Y), AimLineColor);
        }
    }

    private static void DrawGroundReticle(Vector2 target, float elapsedSeconds)
    {
        float outerRadius = ReticleOuterBaseRadius + (MathF.Sin(elapsedSeconds * ReticlePulseSpeed) * ReticlePulseAmplitude);
        var center = new Vector3(target.X, GroundY, target.Y);
        Raylib.DrawCircle3D(center, outerRadius, Vector3.UnitX, FlatGroundRotationDegrees, ReticleOuterColor);
        Raylib.DrawCircle3D(center, ReticleInnerRadius, Vector3.UnitX, FlatGroundRotationDegrees, ReticleInnerColor);
    }
}
