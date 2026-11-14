using System.Numerics;
using Raylib_cs;
using ShadowHeist.GuardAI.States;

namespace ShadowHeist.GuardAI.Rendering;

/// <summary>
/// Renders 3D patrol routes, active A* navigation paths, investigation targets, and acoustic noise perimeter rings.
/// </summary>
public sealed class GuardPathGizmo3DRenderer
{
    private const float PatrolHeightY = 0.9f;
    private const float ActivePathHeightY = 2.2f;
    private const float ActiveNodeHeightY = 2.6f;
    private const float NoiseRingHeightY = 1.6f;
    private const float PatrolLinkRadius = 1.1f;
    private const float WaypointMarkerRadius = 3.5f;
    private const float ActivePathRadius = 1.6f;
    private const float ActiveNodeRadius = 2.4f;
    private const float BaseTargetRingRadius = 10f;
    private const float TargetPulseSpeed = 6f;
    private const float TargetPulseAmplitude = 2.5f;
    private const float MinSegmentLengthSquared = 0.25f;

    private static readonly Color PatrolLineColor = new(100, 116, 139, 90);
    private static readonly Color WaypointDotColor = new(245, 158, 11, 130);
    private static readonly Color SuspiciousPathColor = new(250, 204, 21, 210);
    private static readonly Color AlertPathColor = new(244, 63, 94, 220);
    private static readonly Color NoiseRingColor = new(250, 204, 21, 135);

    private Vector2 _lastNoiseOrigin;
    private float _lastNoiseRadius;

    public void RecordNoisePulse(Vector2 origin, float radius)
    {
        _lastNoiseOrigin = origin;
        _lastNoiseRadius = radius;
    }

    public void Draw(IReadOnlyList<Guard> guards, float elapsedSeconds)
    {
        bool anyFollowingPath = false;

        foreach (Guard guard in guards)
        {
            DrawPatrolRoute(guard.Waypoints);

            if (guard.CurrentState is IGuardPathState pathState)
            {
                anyFollowingPath = true;
                DrawActivePath(guard.Position, pathState, elapsedSeconds);
            }
        }

        if (anyFollowingPath && _lastNoiseRadius > 0f)
        {
            DrawNoisePerimeterRing(_lastNoiseOrigin, _lastNoiseRadius);
        }
    }

    private static void DrawPatrolRoute(IReadOnlyList<Vector2> waypoints)
    {
        for (int waypointIndex = 0; waypointIndex < waypoints.Count; waypointIndex++)
        {
            Vector2 current = waypoints[waypointIndex];
            Vector2 next = waypoints[(waypointIndex + 1) % waypoints.Count];
            var start3D = new Vector3(current.X, PatrolHeightY, current.Y);
            var end3D = new Vector3(next.X, PatrolHeightY, next.Y);

            if (Vector2.DistanceSquared(current, next) > MinSegmentLengthSquared)
            {
                Raylib.DrawCylinderEx(start3D, end3D, PatrolLinkRadius, PatrolLinkRadius, 6, PatrolLineColor);
            }

            Raylib.DrawCylinder(start3D, WaypointMarkerRadius, WaypointMarkerRadius, 0.8f, 10, WaypointDotColor);
        }
    }

    private static void DrawActivePath(Vector2 guardPosition, IGuardPathState pathState, float elapsedSeconds)
    {
        Color pathColor = pathState.StateName == "Alert" ? AlertPathColor : SuspiciousPathColor;
        Vector2 previous = guardPosition;

        for (int waypointIndex = pathState.CurrentPathIndex; waypointIndex < pathState.PathWaypoints.Count; waypointIndex++)
        {
            Vector2 waypoint = pathState.PathWaypoints[waypointIndex];
            if (Vector2.DistanceSquared(previous, waypoint) > MinSegmentLengthSquared)
            {
                var start3D = new Vector3(previous.X, ActivePathHeightY, previous.Y);
                var end3D = new Vector3(waypoint.X, ActivePathHeightY, waypoint.Y);
                Raylib.DrawCylinderEx(start3D, end3D, ActivePathRadius, ActivePathRadius, 6, pathColor);
            }

            Raylib.DrawSphere(new Vector3(waypoint.X, ActiveNodeHeightY, waypoint.Y), ActiveNodeRadius, pathColor);
            previous = waypoint;
        }

        float targetRingRadius = BaseTargetRingRadius + (MathF.Sin(elapsedSeconds * TargetPulseSpeed) * TargetPulseAmplitude);
        var targetCenter = new Vector3(pathState.TargetPosition.X, ActivePathHeightY, pathState.TargetPosition.Y);
        Raylib.DrawCylinderWires(targetCenter, targetRingRadius, targetRingRadius, 0.6f, 20, pathColor);
    }

    private static void DrawNoisePerimeterRing(Vector2 origin, float radius)
    {
        var ringCenter = new Vector3(origin.X, NoiseRingHeightY, origin.Y);
        Raylib.DrawCylinderWires(ringCenter, radius, radius, 0.5f, 36, NoiseRingColor);
    }
}
