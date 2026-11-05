using System.Numerics;
using Raylib_cs;
using ShadowHeist.GuardAI.States;

namespace ShadowHeist.GuardAI.Rendering;

/// <summary>
/// Handles rendering of Guard entities, rotation transforms, and patrol/suspicious path gizmos.
/// </summary>
public sealed class GuardRenderer : IDisposable
{
    private const string GuardTexturePath = "assets/sprites/guard_patrol.png";
    private const float GuardSpriteSize = 48f;
    private const float WaypointDotRadius = 4f;
    private const float InvestigationTargetRadius = 6f;
    private const float ForwardGizmoLength = 30f;
    private const float DegToRad = MathF.PI / 180f;

    private static readonly Color PatrolLineColor = new(100, 116, 139, 120);
    private static readonly Color WaypointDotColor = new(245, 158, 11, 160);
    private static readonly Color SuspiciousPathColor = new(250, 204, 21, 180);
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
        bool anySuspicious = false;

        foreach (var guard in guards)
        {
            if (guard.CurrentState is SuspiciousState)
            {
                anySuspicious = true;
            }

            DrawPatrolGizmos(guard);
            DrawSuspiciousGizmos(guard);

            if (_textureLoaded)
            {
                DrawGuardSprite(guard);
            }
            else
            {
                DrawFallbackGuard(guard);
            }

            DrawForwardFacingGizmo(guard);
        }

        if (anySuspicious && _lastNoiseRadius > 0f)
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

    private void DrawGuardSprite(Guard guard)
    {
        var sourceRect = new Rectangle(0, 0, _guardTexture.Width, _guardTexture.Height);
        var destRect = new Rectangle(guard.Position.X, guard.Position.Y, GuardSpriteSize, GuardSpriteSize);
        var origin = new Vector2(GuardSpriteSize / 2f, GuardSpriteSize / 2f);

        Raylib.DrawTexturePro(_guardTexture, sourceRect, destRect, origin, guard.RotationDegrees, Color.White);
    }

    private static void DrawFallbackGuard(Guard guard)
    {
        Raylib.DrawCircleV(guard.Position, GuardSpriteSize / 2f, Color.Maroon);
        Raylib.DrawCircleLines((int)guard.Position.X, (int)guard.Position.Y, GuardSpriteSize / 2f, Color.Red);
    }

    private static void DrawForwardFacingGizmo(Guard guard)
    {
        float angleRad = guard.RotationDegrees * DegToRad;
        var forward = new Vector2(MathF.Cos(angleRad), MathF.Sin(angleRad));
        var lineEnd = guard.Position + forward * ForwardGizmoLength;

        Color gizmoColor = guard.CurrentState is SuspiciousState ? Color.Yellow : Color.Orange;
        Raylib.DrawLineV(guard.Position, lineEnd, gizmoColor);
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

    private static void DrawSuspiciousGizmos(Guard guard)
    {
        if (guard.CurrentState is not SuspiciousState suspicious)
        {
            return;
        }

        Vector2 previous = guard.Position;
        for (int i = suspicious.CurrentPathIndex; i < suspicious.PathWaypoints.Count; i++)
        {
            Vector2 current = suspicious.PathWaypoints[i];
            Raylib.DrawLineV(previous, current, SuspiciousPathColor);
            previous = current;
        }

        Raylib.DrawCircleLines(
            (int)suspicious.InvestigationTarget.X,
            (int)suspicious.InvestigationTarget.Y,
            InvestigationTargetRadius,
            SuspiciousPathColor);
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
