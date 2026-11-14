using System.Numerics;
using Raylib_cs;
using ShadowHeist.GuardAI.States;

namespace ShadowHeist.GuardAI.Rendering;

/// <summary>
/// Handles rendering of Guard entities, rotation transforms, and patrol/pathfinding gizmos.
/// </summary>
public sealed class GuardRenderer : IDisposable
{
    private const string GuardTexturePath = "assets/sprites/guard_patrol.png";
    private const float GuardSpriteSize = 48f;
    private const float WaypointDotRadius = 4f;
    private const float TargetRingRadius = 6f;
    private const float ForwardGizmoLength = 30f;
    private const float DegToRad = MathF.PI / 180f;

    private static readonly Color PatrolLineColor = new(100, 116, 139, 120);
    private static readonly Color WaypointDotColor = new(245, 158, 11, 160);
    private static readonly Color ActivePathColor = new(250, 204, 21, 180);
    private static readonly Color NoiseRingColor = new(250, 204, 21, 110);

    private Texture2D _guardTexture;
    private bool _textureLoaded;
    private Vector2 _lastNoiseOrigin;
    private float _lastNoiseRadius;

    public GuardRenderer()
    {
        LoadTexture();
    }

    public void RecordNoisePulse(Vector2 origin, float radius)
    {
        _lastNoiseOrigin = origin;
        _lastNoiseRadius = radius;
    }

    public void Draw(IEnumerable<Guard> guards)
    {
        bool anyFollowingPath = false;

        foreach (var guard in guards)
        {
            DrawPatrolGizmos(guard);

            if (guard.CurrentState is IGuardPathState pathState)
            {
                anyFollowingPath = true;
                DrawActivePathGizmos(guard.Position, pathState);
            }

            DrawGuardBody(guard);
            DrawForwardFacingGizmo(guard, guard.CurrentState is IGuardPathState);
        }

        if (anyFollowingPath && _lastNoiseRadius > 0f)
        {
            Raylib.DrawCircleLines((int)_lastNoiseOrigin.X, (int)_lastNoiseOrigin.Y, _lastNoiseRadius, NoiseRingColor);
        }
    }

    public void Dispose()
    {
        if (_textureLoaded)
        {
            Raylib.UnloadTexture(_guardTexture);
            _textureLoaded = false;
        }
    }

    private void DrawGuardBody(Guard guard)
    {
        if (_textureLoaded)
        {
            var sourceRect = new Rectangle(0, 0, _guardTexture.Width, _guardTexture.Height);
            var destRect = new Rectangle(guard.Position.X, guard.Position.Y, GuardSpriteSize, GuardSpriteSize);
            var origin = new Vector2(GuardSpriteSize / 2f, GuardSpriteSize / 2f);
            Raylib.DrawTexturePro(_guardTexture, sourceRect, destRect, origin, guard.RotationDegrees, Color.White);
            return;
        }

        Raylib.DrawCircleV(guard.Position, GuardSpriteSize / 2f, Color.Maroon);
        Raylib.DrawCircleLines((int)guard.Position.X, (int)guard.Position.Y, GuardSpriteSize / 2f, Color.Red);
    }

    private static void DrawForwardFacingGizmo(Guard guard, bool isAlertedPath)
    {
        float angleRad = guard.RotationDegrees * DegToRad;
        var forward = new Vector2(MathF.Cos(angleRad), MathF.Sin(angleRad));
        var lineEnd = guard.Position + forward * ForwardGizmoLength;

        Raylib.DrawLineV(guard.Position, lineEnd, isAlertedPath ? Color.Yellow : Color.Orange);
    }

    private static void DrawPatrolGizmos(Guard guard)
    {
        var waypoints = guard.Waypoints;
        if (waypoints.Count < 2)
        {
            return;
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            var current = waypoints[i];
            var next = waypoints[(i + 1) % waypoints.Count];

            Raylib.DrawLineV(current, next, PatrolLineColor);
            Raylib.DrawCircleV(current, WaypointDotRadius, WaypointDotColor);
        }
    }

    private static void DrawActivePathGizmos(Vector2 guardPos, IGuardPathState pathState)
    {
        Vector2 previous = guardPos;
        for (int i = pathState.CurrentPathIndex; i < pathState.PathWaypoints.Count; i++)
        {
            Vector2 current = pathState.PathWaypoints[i];
            Raylib.DrawLineV(previous, current, ActivePathColor);
            previous = current;
        }

        Raylib.DrawCircleLines(
            (int)pathState.TargetPosition.X,
            (int)pathState.TargetPosition.Y,
            TargetRingRadius,
            ActivePathColor);
    }

    private void LoadTexture()
    {
        if (Raylib.IsWindowReady() && File.Exists(GuardTexturePath))
        {
            _guardTexture = Raylib.LoadTexture(GuardTexturePath);
            _textureLoaded = true;
        }
    }
}
