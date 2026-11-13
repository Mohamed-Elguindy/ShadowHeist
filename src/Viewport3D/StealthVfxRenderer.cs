using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld.Interactables;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders 3D stealth VFX: wall-clipped flashlight cones, sound ripples, lamp pools, and loot sparks.
/// </summary>
public sealed class StealthVfxRenderer
{
    private const int MaxRipples = 16;
    private const float RippleLifetimeSeconds = 0.85f;
    private const float SparkLifetimeSeconds = 0.90f;

    private readonly List<SoundRipple> _ripples = new();
    private readonly List<GoldSpark> _sparks = new();
    private readonly HashSet<string> _collectedCrates = new();

    public void SubscribeEvents(GameEventBus eventBus) =>
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);

    public void Update(float deltaTime, GameContext context)
    {
        for (int i = _ripples.Count - 1; i >= 0; i--)
        {
            SoundRipple r = _ripples[i];
            r.Age += deltaTime;
            if (r.Age >= RippleLifetimeSeconds) _ripples.RemoveAt(i);
            else _ripples[i] = r;
        }

        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            GoldSpark s = _sparks[i];
            s.Age += deltaTime;
            s.Position += s.Velocity * deltaTime;
            s.Velocity += new Vector3(0f, -180f, 0f) * deltaTime;
            if (s.Age >= SparkLifetimeSeconds) _sparks.RemoveAt(i);
            else _sparks[i] = s;
        }

        foreach (LootCrate crate in context.Interactables.OfType<LootCrate>())
        {
            if (crate.IsCollected && _collectedCrates.Add(crate.Id)) SpawnLootBurst(crate.Position);
        }
    }

    public void Draw(IReadOnlyList<RectangleBounds> walls, GameContext context, IReadOnlyList<Guard> guards, float timeSeconds)
    {
        DrawLampGlows();
        DrawVaultSecurityLasers(timeSeconds);
        DrawRipples();
        DrawSparks();
        DrawWallClippedCones(guards, walls, context.IsAlarmTriggered, timeSeconds);
    }

    private void OnNoiseEmitted(NoiseEmittedEvent evt)
    {
        if (_ripples.Count >= MaxRipples) _ripples.RemoveAt(0);
        _ripples.Add(new SoundRipple(evt.Origin, MathF.Max(evt.Radius, 40f), 0f));
    }

    private void SpawnLootBurst(Vector2 origin)
    {
        for (int i = 0; i < 14; i++)
        {
            float ang = (i / 14f) * MathF.PI * 2f;
            Vector3 vel = new(MathF.Cos(ang) * 55f, 95f + (i % 3) * 28f, MathF.Sin(ang) * 55f);
            _sparks.Add(new GoldSpark(new Vector3(origin.X, 24f, origin.Y), vel, 0f));
        }
    }

    private static void DrawLampGlows()
    {
        foreach (FurnitureSpec spec in Viewport3DConfig.LobbyFurniture)
        {
            if (!spec.EmitsWarmGlow) continue;
            var center = new Vector3(spec.Position.X, 0.6f, spec.Position.Y);
            Raylib.DrawCylinder(center, 95f, 95f, 0.4f, 20, new Color(255, 214, 120, 34));
            Raylib.DrawCylinder(center, 46f, 46f, 0.6f, 16, new Color(255, 224, 150, 56));
            Raylib.DrawSphere(new Vector3(spec.Position.X, 66f, spec.Position.Y), 7f, new Color(255, 236, 168, 225));
        }
    }

    private static void DrawVaultSecurityLasers(float timeSeconds)
    {
        byte alpha = (byte)(150 + 75 * MathF.Sin(timeSeconds * 4.5f));
        Color beam = new((byte)244, (byte)63, (byte)94, alpha);
        for (int i = 0; i < 3; i++) Raylib.DrawLine3D(new Vector3(900f, 76f + i * 7f, 236f), new Vector3(900f, 76f + i * 7f, 324f), beam);
    }

    private void DrawRipples()
    {
        foreach (SoundRipple r in _ripples)
        {
            float t = r.Age / RippleLifetimeSeconds;
            float radius = 14f + (r.MaxRadius - 14f) * t;
            Raylib.DrawCylinderWires(new Vector3(r.Origin.X, 2f, r.Origin.Y), radius, radius, 0.6f, 28, new Color((byte)56, (byte)225, (byte)255, (byte)(215 * (1f - t))));
        }
    }

    private void DrawSparks()
    {
        foreach (GoldSpark s in _sparks)
        {
            float t = s.Age / SparkLifetimeSeconds;
            Raylib.DrawCube(s.Position, 5f, 5f, 5f, new Color((byte)248, (byte)196, (byte)44, (byte)(255 * (1f - t))));
        }
    }

    private static void DrawWallClippedCones(IReadOnlyList<Guard> guards, IReadOnlyList<RectangleBounds> walls, bool alarm, float t)
    {
        Color fill = alarm ? new Color((byte)244, (byte)63, (byte)94, (byte)(95 + 35 * MathF.Sin(t * 8f))) : new Color(255, 214, 78, 108);
        Color edge = alarm ? new Color(255, 110, 125, 220) : new Color(255, 238, 145, 210);
        foreach (Guard guard in guards) DrawGuardCone(guard, walls, fill, edge);
    }

    private static void DrawGuardCone(Guard guard, IReadOnlyList<RectangleBounds> walls, Color fill, Color edge)
    {
        Vector2 origin = guard.Position;
        var apex = new Vector3(origin.X, 2f, origin.Y);
        var hand = new Vector3(origin.X, 36f, origin.Y);
        float startDeg = guard.RotationDegrees - Viewport3DConfig.VisionHalfAngleDegrees;
        float stepDeg = (Viewport3DConfig.VisionHalfAngleDegrees * 2f) / Viewport3DConfig.VisionRayCount;
        Vector3? prevHit = null;

        for (int i = 0; i <= Viewport3DConfig.VisionRayCount; i++)
        {
            float rad = (startDeg + stepDeg * i) * Raylib.DEG2RAD;
            Vector2 dir = new(MathF.Cos(rad), MathF.Sin(rad));
            float dist = WallRaycastHelper.CastRayAgainstWalls(origin, dir, Viewport3DConfig.VisionRange, walls);
            var hit = new Vector3(origin.X + dir.X * dist, 2f, origin.Y + dir.Y * dist);

            if (prevHit is Vector3 prev)
            {
                Raylib.DrawTriangle3D(apex, hit, prev, fill);
                Raylib.DrawTriangle3D(apex, prev, hit, fill);
                Raylib.DrawLine3D(prev, hit, edge);
            }

            if (i == 0 || i == Viewport3DConfig.VisionRayCount)
            {
                Raylib.DrawLine3D(apex, hit, edge);
                Raylib.DrawLine3D(hand, hit, new Color(edge.R, edge.G, edge.B, (byte)80));
            }

            prevHit = hit;
        }
    }

    private struct SoundRipple(Vector2 origin, float maxRadius, float age)
    { public Vector2 Origin { get; } = origin; public float MaxRadius { get; } = maxRadius; public float Age { get; set; } = age; }

    private struct GoldSpark(Vector3 pos, Vector3 vel, float age)
    { public Vector3 Position { get; set; } = pos; public Vector3 Velocity { get; set; } = vel; public float Age { get; set; } = age; }
}
