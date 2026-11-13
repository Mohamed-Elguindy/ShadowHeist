using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.GuardAI;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders 3D stealth VFX: wall-clipped guard flashlight cones, sound ripples, and warm lobby lamp pools.
/// </summary>
public sealed class StealthVfxRenderer
{
    private const int MaxRipples = 16;
    private const float RippleLifetimeSeconds = 0.85f;
    private const float LampGlowHeight = 0.6f;

    private readonly List<SoundRipple> _ripples = new();

    public void SubscribeEvents(GameEventBus eventBus) =>
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);

    public void Update(float deltaTime)
    {
        for (int i = _ripples.Count - 1; i >= 0; i--)
        {
            SoundRipple ripple = _ripples[i];
            ripple.Age += deltaTime;
            if (ripple.Age >= RippleLifetimeSeconds)
                _ripples.RemoveAt(i);
            else
                _ripples[i] = ripple;
        }
    }

    public void Draw(GameContext context, IReadOnlyList<Guard> guards, float timeSeconds)
    {
        DrawLampGlows();
        DrawVaultSecurityLasers(timeSeconds);
        DrawRipples();
        DrawWallClippedCones(guards, context.Walls, context.IsAlarmTriggered, timeSeconds);
    }

    private void OnNoiseEmitted(NoiseEmittedEvent evt)
    {
        if (_ripples.Count >= MaxRipples) _ripples.RemoveAt(0);
        _ripples.Add(new SoundRipple(evt.Origin, MathF.Max(evt.Radius, 40f), 0f));
    }

    private static void DrawLampGlows()
    {
        foreach (FurnitureSpec spec in Viewport3DConfig.LobbyFurniture)
        {
            if (!spec.EmitsWarmGlow) continue;
            var center = new Vector3(spec.Position.X, LampGlowHeight, spec.Position.Y);
            Raylib.DrawCylinder(center, 90f, 90f, 0.4f, 20, new Color(255, 214, 120, 32));
            Raylib.DrawCylinder(center, 44f, 44f, 0.6f, 16, new Color(255, 224, 150, 52));
            Raylib.DrawSphere(new Vector3(spec.Position.X, 66f, spec.Position.Y), 7f, new Color(255, 236, 168, 225));
        }
    }

    private static void DrawVaultSecurityLasers(float timeSeconds)
    {
        byte alpha = (byte)(150 + 75 * MathF.Sin(timeSeconds * 4.5f));
        Color beam = new((byte)244, (byte)63, (byte)94, alpha);
        for (int i = 0; i < 3; i++)
        {
            float y = 76f + i * 7f;
            Raylib.DrawLine3D(new Vector3(900f, y, 236f), new Vector3(900f, y, 324f), beam);
        }
    }

    private void DrawRipples()
    {
        foreach (SoundRipple ripple in _ripples)
        {
            float t = ripple.Age / RippleLifetimeSeconds;
            float radius = 14f + (ripple.MaxRadius - 14f) * t;
            byte alpha = (byte)(215 * (1f - t));
            var center = new Vector3(ripple.Origin.X, 2f, ripple.Origin.Y);
            Raylib.DrawCylinderWires(center, radius, radius, 0.6f, 28, new Color((byte)56, (byte)225, (byte)255, alpha));
        }
    }

    private static void DrawWallClippedCones(
        IReadOnlyList<Guard> guards,
        IReadOnlyList<RectangleBounds> walls,
        bool isAlarmTriggered,
        float timeSeconds)
    {
        Color floorTint = isAlarmTriggered
            ? new Color((byte)244, (byte)63, (byte)94, (byte)(95 + 35 * MathF.Sin(timeSeconds * 8f)))
            : new Color(255, 214, 78, 108);
        Color edgeTint = isAlarmTriggered ? new Color(255, 110, 125, 220) : new Color(255, 238, 145, 210);

        foreach (Guard guard in guards)
        {
            DrawSingleGuardCone(guard, walls, floorTint, edgeTint);
        }
    }

    private static void DrawSingleGuardCone(Guard guard, IReadOnlyList<RectangleBounds> walls, Color floorTint, Color edgeTint)
    {
        Vector2 origin2D = guard.Position;
        var apexGround = new Vector3(origin2D.X, 2f, origin2D.Y);
        var flashlightOrigin = new Vector3(origin2D.X, 36f, origin2D.Y);
        float startDeg = guard.RotationDegrees - Viewport3DConfig.VisionHalfAngleDegrees;
        float stepDeg = (Viewport3DConfig.VisionHalfAngleDegrees * 2f) / Viewport3DConfig.VisionRayCount;
        Vector3? prevHit = null;

        for (int i = 0; i <= Viewport3DConfig.VisionRayCount; i++)
        {
            float rad = (startDeg + stepDeg * i) * Raylib.DEG2RAD;
            Vector2 dir = new(MathF.Cos(rad), MathF.Sin(rad));
            float hitDist = WallRaycastHelper.CastRayAgainstWalls(origin2D, dir, Viewport3DConfig.VisionRange, walls);
            var hitGround = new Vector3(origin2D.X + dir.X * hitDist, 2f, origin2D.Y + dir.Y * hitDist);

            if (prevHit is Vector3 prev)
            {
                Raylib.DrawTriangle3D(apexGround, hitGround, prev, floorTint);
                Raylib.DrawTriangle3D(apexGround, prev, hitGround, floorTint);
                Raylib.DrawLine3D(prev, hitGround, edgeTint);
            }

            if (i == 0 || i == Viewport3DConfig.VisionRayCount)
            {
                Raylib.DrawLine3D(apexGround, hitGround, edgeTint);
                Raylib.DrawLine3D(flashlightOrigin, hitGround, new Color(edgeTint.R, edgeTint.G, edgeTint.B, (byte)80));
            }

            prevHit = hitGround;
        }
    }

    private struct SoundRipple
    {
        public Vector2 Origin { get; }
        public float MaxRadius { get; }
        public float Age { get; set; }

        public SoundRipple(Vector2 origin, float maxRadius, float age)
        {
            Origin = origin;
            MaxRadius = maxRadius;
            Age = age;
        }
    }
}
