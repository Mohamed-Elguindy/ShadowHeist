using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.GuardAI;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders wall-clipped 3D vision cones for guards and the player alongside the 3D ground aim reticle.
/// </summary>
public sealed class VisionCone3DRenderer : ISceneRenderPass
{
    public void Draw(in Scene3DRenderFrame frame)
    {
        DrawPlayerVisionCone(frame.Context.PlayerPosition, frame.PlayerRotationDegrees, frame.VisionBlockers);
        DrawGuardCones(frame.Guards, frame.VisionBlockers, frame.Context.IsAlarmTriggered, frame.ElapsedSeconds);
        DrawAimReticle(frame.Context.PlayerPosition, frame.Context.AimWorldOverride, frame.ElapsedSeconds);
    }

    private static void DrawPlayerVisionCone(
        Vector2 playerPosition,
        float playerRotationDegrees,
        IReadOnlyList<RectangleBounds> visionBlockers)
    {
        IReadOnlyList<Vector2> cone = VisionRaycaster.ComputeVisionCone(
            playerPosition,
            playerRotationDegrees,
            visionBlockers,
            VisionRaycaster.DefaultFovDegrees,
            VisionRaycaster.MaxVisionDist,
            VisionRaycaster.DefaultRayCount);

        DrawProjectedVisionPolygon(
            cone,
            groundY: 2.3f,
            sourceY: 34f,
            fill: new Color(56, 225, 255, 36),
            edge: new Color(110, 235, 255, 115));
    }

    private static void DrawGuardCones(
        IReadOnlyList<Guard> guards,
        IReadOnlyList<RectangleBounds> visionBlockers,
        bool alarmActive,
        float timeSeconds)
    {
        foreach (Guard guard in guards)
        {
            (Color fill, Color edge) = ResolveGuardConeColors(guard.CurrentState.StateName, alarmActive, timeSeconds);
            IReadOnlyList<Vector2> cone = VisionRaycaster.ComputeVisionCone(
                guard.Position,
                guard.RotationDegrees,
                visionBlockers,
                fovDegrees: Viewport3DConfig.VisionHalfAngleDegrees * 2f,
                maxDistance: Viewport3DConfig.VisionRange,
                rayCount: Viewport3DConfig.VisionRayCount);

            DrawProjectedVisionPolygon(cone, groundY: 2.0f, sourceY: 36f, fill, edge);
        }
    }

    private static (Color Fill, Color Edge) ResolveGuardConeColors(string stateName, bool alarmActive, float timeSeconds)
    {
        if (alarmActive || stateName == "Alert")
        {
            byte pulseAlpha = (byte)(95f + (35f * MathF.Sin(timeSeconds * 8f)));
            return (new Color((byte)244, (byte)63, (byte)94, pulseAlpha), new Color(255, 110, 125, 220));
        }

        if (stateName == "Suspicious")
        {
            return (new Color(251, 146, 60, 112), new Color(253, 186, 116, 215));
        }

        return (new Color(255, 214, 78, 108), new Color(255, 238, 145, 210));
    }

    private static void DrawProjectedVisionPolygon(
        IReadOnlyList<Vector2> vertices,
        float groundY,
        float sourceY,
        Color fill,
        Color edge)
    {
        if (vertices.Count < 3)
        {
            return;
        }

        var apex = new Vector3(vertices[0].X, groundY, vertices[0].Y);
        var beamOrigin = new Vector3(vertices[0].X, sourceY, vertices[0].Y);
        var beamEdgeColor = new Color(edge.R, edge.G, edge.B, (byte)(edge.A * 0.4f));

        for (int i = 1; i < vertices.Count - 1; i++)
        {
            var prev = new Vector3(vertices[i].X, groundY, vertices[i].Y);
            var hit = new Vector3(vertices[i + 1].X, groundY, vertices[i + 1].Y);
            Raylib.DrawTriangle3D(apex, hit, prev, fill);
            Raylib.DrawTriangle3D(apex, prev, hit, fill);
            Raylib.DrawLine3D(prev, hit, edge);
        }

        var first = new Vector3(vertices[1].X, groundY, vertices[1].Y);
        var last = new Vector3(vertices[^1].X, groundY, vertices[^1].Y);
        Raylib.DrawLine3D(apex, first, edge);
        Raylib.DrawLine3D(apex, last, edge);
        Raylib.DrawLine3D(beamOrigin, first, beamEdgeColor);
        Raylib.DrawLine3D(beamOrigin, last, beamEdgeColor);
    }

    private static void DrawAimReticle(Vector2 playerPosition, Vector2? aimPoint, float timeSeconds)
    {
        if (aimPoint is not Vector2 aim)
        {
            return;
        }

        float pulseRadius = 10f + (MathF.Sin(timeSeconds * 5f) * 1.5f);
        Raylib.DrawCylinderWires(new Vector3(aim.X, 1.5f, aim.Y), pulseRadius, pulseRadius, 0.5f, 18, new Color(56, 225, 255, 190));
        Raylib.DrawLine3D(new Vector3(playerPosition.X, 22f, playerPosition.Y), new Vector3(aim.X, 2f, aim.Y), new Color(56, 225, 255, 65));
    }
}
