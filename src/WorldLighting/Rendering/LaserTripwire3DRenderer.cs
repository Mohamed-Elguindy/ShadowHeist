using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core.Rendering3D;

namespace ShadowHeist.WorldLighting.Rendering;

/// <summary>
/// Renders 3D security laser tripwire emitter pylons, dual volumetric laser beams, floor warning traces, and wall impact seals.
/// </summary>
public sealed class LaserTripwire3DRenderer
{
    private const float PylonHeight = 50f;
    private const float PylonWidth = 10f;
    private const float PylonDepth = 12f;
    private const float LowerBeamHeight = 22f;
    private const float UpperBeamHeight = 38f;
    private const float FloorTraceHeight = 1.4f;
    private const float GlowRadius = 2.1f;
    private const float CoreRadius = 0.7f;
    private const float BeamLengthEpsilonSq = 0.25f;

    private static readonly Color PylonBodyColor = new(44, 52, 68, 255);
    private static readonly Color PylonTrimColor = new(78, 90, 112, 255);
    private static readonly Color InactiveLedColor = new(74, 222, 128, 230);
    private static readonly Color FloorTraceColor = new(244, 63, 94, 130);

    public void Draw(IReadOnlyList<LaserTripwire> lasers, float elapsedSeconds)
    {
        foreach (LaserTripwire laser in lasers)
        {
            DrawEmitterPylon(laser, elapsedSeconds);

            if (!laser.IsActive || Vector2.DistanceSquared(laser.Origin, laser.BeamEnd) < BeamLengthEpsilonSq)
            {
                continue;
            }

            DrawDualLaserBeams(laser.Origin, laser.BeamEnd, laser.HasTriggeredAlarm, elapsedSeconds);
        }
    }

    private static void DrawEmitterPylon(LaserTripwire laser, float timeSeconds)
    {
        Raylib3DPrimitives.DrawGroundShadow(laser.Origin, 10f);

        var pylonCenter = new Vector3(laser.Origin.X, PylonHeight * 0.5f, laser.Origin.Y);
        Raylib3DPrimitives.DrawOutlinedCube(pylonCenter, new Vector3(PylonWidth, PylonHeight, PylonDepth), PylonBodyColor, Viewport3DConfig.OutlineColor);

        var capCenter = new Vector3(laser.Origin.X, PylonHeight + 2f, laser.Origin.Y);
        Raylib.DrawCube(capCenter, PylonWidth + 2f, 4f, PylonDepth + 2f, PylonTrimColor);

        Color ledColor = ResolveLedColor(laser, timeSeconds);
        Raylib.DrawSphere(new Vector3(laser.Origin.X, PylonHeight + 6f, laser.Origin.Y), 3.2f, ledColor);

        Vector3 nozzleOffset = new Vector3(laser.Direction.X, 0f, laser.Direction.Y) * 5.5f;
        Raylib.DrawSphere(new Vector3(laser.Origin.X, LowerBeamHeight, laser.Origin.Y) + nozzleOffset, 2.2f, ledColor);
        Raylib.DrawSphere(new Vector3(laser.Origin.X, UpperBeamHeight, laser.Origin.Y) + nozzleOffset, 2.2f, ledColor);
    }

    private static void DrawDualLaserBeams(Vector2 origin, Vector2 beamEnd, bool triggered, float timeSeconds)
    {
        float pulseSpeed = triggered ? 12f : 5.5f;
        float pulse = 0.72f + (0.28f * MathF.Sin(timeSeconds * pulseSpeed));
        byte coreAlpha = (byte)(220f + (35f * pulse));
        byte glowAlpha = (byte)(92f * pulse);

        var glowColor = new Color((byte)244, (byte)63, (byte)94, glowAlpha);
        var coreColor = new Color((byte)255, triggered ? (byte)150 : (byte)96, triggered ? (byte)160 : (byte)108, coreAlpha);

        DrawSingleBeamAtHeight(origin, beamEnd, LowerBeamHeight, glowColor, coreColor);
        DrawSingleBeamAtHeight(origin, beamEnd, UpperBeamHeight, glowColor, coreColor);

        Raylib.DrawLine3D(
            new Vector3(origin.X, FloorTraceHeight, origin.Y),
            new Vector3(beamEnd.X, FloorTraceHeight, beamEnd.Y),
            FloorTraceColor);
    }

    private static void DrawSingleBeamAtHeight(Vector2 origin, Vector2 beamEnd, float height, Color glowColor, Color coreColor)
    {
        var start = new Vector3(origin.X, height, origin.Y);
        var end = new Vector3(beamEnd.X, height, beamEnd.Y);

        Raylib3DPrimitives.DrawVolumetricBeam(start, end, GlowRadius, CoreRadius, glowColor, coreColor);
        Raylib.DrawSphere(end, 3.0f, glowColor);
        Raylib.DrawSphere(end, 1.4f, coreColor);
    }

    private static Color ResolveLedColor(LaserTripwire laser, float timeSeconds)
    {
        if (!laser.IsActive)
        {
            return InactiveLedColor;
        }

        float frequency = laser.HasTriggeredAlarm ? 11f : 4f;
        byte alpha = (byte)(165f + (90f * MathF.Sin(timeSeconds * frequency)));
        return new Color((byte)244, (byte)63, (byte)94, alpha);
    }
}
