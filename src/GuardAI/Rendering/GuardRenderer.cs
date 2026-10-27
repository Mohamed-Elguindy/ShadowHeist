using System.Numerics;
using Raylib_cs;

namespace ShadowHeist.GuardAI.Rendering;

/// <summary>
/// Handles rendering of Guard entities, rotation transforms, and patrol path gizmos.
/// </summary>
public sealed class GuardRenderer : IDisposable
{
    private const string GuardTexturePath = "assets/sprites/guard_patrol.png";
    private const float GuardSpriteSize = 48f;
    private const float WaypointDotRadius = 4f;

    private Texture2D _guardTexture;
    private bool _textureLoaded;

    public GuardRenderer()
    {
        LoadTexture();
    }

    public void Draw(IEnumerable<Guard> guards)
    {
        foreach (var guard in guards)
        {
            DrawPatrolGizmos(guard);

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
        float angleRad = guard.RotationDegrees * (MathF.PI / 180f);
        var forward = new Vector2(MathF.Cos(angleRad), MathF.Sin(angleRad));
        var lineEnd = guard.Position + forward * 30f;

        Raylib.DrawLineV(guard.Position, lineEnd, Color.Orange);
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

            Raylib.DrawLineV(current, next, new Color(100, 116, 139, 120));
            Raylib.DrawCircleV(current, WaypointDotRadius, new Color(245, 158, 11, 160));
        }
    }

    private void LoadTexture()
    {
        if (File.Exists(GuardTexturePath))
        {
            _guardTexture = Raylib.LoadTexture(GuardTexturePath);
            _textureLoaded = true;
        }
    }
}
