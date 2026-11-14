using System.Numerics;
using Raylib_cs;
using ShadowHeist.Core;
using ShadowHeist.Core.Events;
using ShadowHeist.GuardAI;
using ShadowHeist.HeistWorld.Interactables;
using ShadowHeist.WorldLighting;

namespace ShadowHeist.Viewport3D;

/// <summary>
/// Renders 3D stealth VFX: wall-clipped guard & player vision cones via VisionRaycaster, category-coded sound ripples, lamp pools, loot sparks, and aim reticle.
/// </summary>
public sealed class StealthVfxRenderer : ISceneRenderPass
{
    private const int MaxRipples = 16;
    private const float RippleLifetimeSeconds = 0.85f;
    private const float SparkLifetimeSeconds = 0.90f;
    private const float LoudSprintNoiseThreshold = 180f;

    private readonly List<SoundRipple> _ripples = new();
    private readonly List<GoldSpark> _sparks = new();
    private readonly HashSet<string> _collectedCrates = new();

    public void SubscribeEvents(GameEventBus eventBus)
    {
        _ripples.Clear();
        _sparks.Clear();
        _collectedCrates.Clear();
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Update(float deltaTime, GameContext context)
    {
        UpdateRipples(deltaTime);
        UpdateSparks(deltaTime);
        UpdateLootBursts(context);
    }

    public void Draw(in Scene3DRenderFrame frame)
    {
        DrawLampGlows();
        DrawVaultSecuritySeal(frame.Context, frame.ElapsedSeconds);
        DrawAcousticRipples();
        DrawLootSparks();
        DrawPlayerVisionCone(frame.Context.PlayerPosition, frame.PlayerRotationDegrees, frame.VisionBlockers);
        DrawGuardCones(frame.Guards, frame.VisionBlockers, frame.Context.IsAlarmTriggered, frame.ElapsedSeconds);
        DrawAimReticle(frame.Context.PlayerPosition, frame.Context.AimWorldOverride, frame.ElapsedSeconds);
    }

    private void UpdateRipples(float deltaTime)
    {
        for (int i = _ripples.Count - 1; i >= 0; i--)
        {
            SoundRipple ripple = _ripples[i];
            ripple.Age += deltaTime;
            if (ripple.Age >= RippleLifetimeSeconds)
            {
                _ripples.RemoveAt(i);
            }
            else
            {
                _ripples[i] = ripple;
            }
        }
    }

    private void UpdateSparks(float deltaTime)
    {
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            GoldSpark spark = _sparks[i];
            spark.Age += deltaTime;
            spark.Position += spark.Velocity * deltaTime;
            spark.Velocity += new Vector3(0f, -180f, 0f) * deltaTime;

            if (spark.Age >= SparkLifetimeSeconds)
            {
                _sparks.RemoveAt(i);
            }
            else
            {
                _sparks[i] = spark;
            }
        }
    }

    private void UpdateLootBursts(GameContext context)
    {
        if (_collectedCrates.Count > 0 && context.StolenCash == 0)
        {
            _collectedCrates.Clear();
        }

        foreach (LootCrate crate in context.Interactables.OfType<LootCrate>())
        {
            if (crate.IsCollected && _collectedCrates.Add(crate.Id))
            {
                SpawnLootBurst(crate.Position);
            }
        }
    }

    private void OnNoiseEmitted(NoiseEmittedEvent noiseEvent)
    {
        if (noiseEvent.Radius <= 0f)
        {
            return;
        }

        if (_ripples.Count >= MaxRipples)
        {
            _ripples.RemoveAt(0);
        }

        _ripples.Add(new SoundRipple(
            noiseEvent.Origin,
            MathF.Max(noiseEvent.Radius, 40f),
            ResolveRippleColor(noiseEvent),
            age: 0f));
    }

    private static Color ResolveRippleColor(NoiseEmittedEvent noiseEvent) => noiseEvent.Category switch
    {
        NoiseCategory.Distraction => new Color(216, 180, 254, 255),
        NoiseCategory.Environment => new Color(244, 63, 94, 255),
        _ when noiseEvent.Radius >= LoudSprintNoiseThreshold => new Color(251, 146, 60, 255),
        _ => new Color(56, 225, 255, 255)
    };

    private void SpawnLootBurst(Vector2 origin)
    {
        const int sparkCount = 14;
        for (int i = 0; i < sparkCount; i++)
        {
            float angle = (i / (float)sparkCount) * MathF.PI * 2f;
            var velocity = new Vector3(MathF.Cos(angle) * 55f, 95f + ((i % 3) * 28f), MathF.Sin(angle) * 55f);
            _sparks.Add(new GoldSpark(new Vector3(origin.X, 24f, origin.Y), velocity, age: 0f));
        }
    }

    private static void DrawLampGlows()
    {
        foreach (FurnitureSpec spec in BankFurnitureLayout.LobbyFurniture)
        {
            if (!spec.EmitsWarmGlow)
            {
                continue;
            }

            var center = new Vector3(spec.Position.X, 0.6f, spec.Position.Y);
            Raylib.DrawCylinder(center, 95f, 95f, 0.4f, 20, new Color(255, 214, 120, 34));
            Raylib.DrawCylinder(center, 46f, 46f, 0.6f, 16, new Color(255, 224, 150, 56));
            Raylib.DrawSphere(new Vector3(spec.Position.X, 66f, spec.Position.Y), 7f, new Color(255, 236, 168, 225));
        }
    }

    private static void DrawVaultSecuritySeal(GameContext context, float timeSeconds)
    {
        VaultDoor? door = context.Interactables.OfType<VaultDoor>().FirstOrDefault();
        if (door is not { IsLocked: true })
        {
            return;
        }

        byte glowAlpha = (byte)(95f + (50f * MathF.Sin(timeSeconds * 4.5f)));
        byte coreAlpha = (byte)(190f + (60f * MathF.Sin(timeSeconds * 4.5f)));
        var glow = new Color((byte)244, (byte)63, (byte)94, glowAlpha);
        var core = new Color((byte)255, (byte)130, (byte)150, coreAlpha);

        for (int i = 0; i < 3; i++)
        {
            float beamHeight = 24f + (i * 18f);
            var start = new Vector3(door.Position.X - 16f, beamHeight, door.Position.Y - 40f);
            var end = new Vector3(door.Position.X - 16f, beamHeight, door.Position.Y + 40f);
            Raylib3DPrimitives.DrawVolumetricBeam(start, end, glowRadius: 1.4f, coreRadius: 0.45f, glow, core);
        }
    }

    private void DrawAcousticRipples()
    {
        foreach (SoundRipple ripple in _ripples)
        {
            float progress = ripple.Age / RippleLifetimeSeconds;
            float radius = 14f + ((ripple.MaxRadius - 14f) * progress);
            byte alpha = (byte)(215f * (1f - progress));
            var ringColor = new Color(ripple.BaseColor.R, ripple.BaseColor.G, ripple.BaseColor.B, alpha);

            Raylib.DrawCylinderWires(new Vector3(ripple.Origin.X, 2f, ripple.Origin.Y), radius, radius, 0.6f, 28, ringColor);
        }
    }

    private void DrawLootSparks()
    {
        foreach (GoldSpark spark in _sparks)
        {
            float progress = spark.Age / SparkLifetimeSeconds;
            byte alpha = (byte)(255f * (1f - progress));
            Raylib.DrawCube(spark.Position, 5f, 5f, 5f, new Color((byte)248, (byte)196, (byte)44, alpha));
        }
    }

    private static void DrawAimReticle(Vector2 playerPos, Vector2? aimPoint, float timeSeconds)
    {
        if (aimPoint is not Vector2 aim)
        {
            return;
        }

        float pulseRadius = 10f + (MathF.Sin(timeSeconds * 5f) * 1.5f);
        Raylib.DrawCylinderWires(new Vector3(aim.X, 1.5f, aim.Y), pulseRadius, pulseRadius, 0.5f, 18, new Color(56, 225, 255, 190));
        Raylib.DrawLine3D(new Vector3(playerPos.X, 22f, playerPos.Y), new Vector3(aim.X, 2f, aim.Y), new Color(56, 225, 255, 65));
    }

    private static void DrawPlayerVisionCone(Vector2 playerPos, float playerRotationDegrees, IReadOnlyList<RectangleBounds> visionBlockers)
    {
        IReadOnlyList<Vector2> cone = VisionRaycaster.ComputeVisionCone(
            playerPos,
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

    private static void DrawGuardCones(IReadOnlyList<Guard> guards, IReadOnlyList<RectangleBounds> visionBlockers, bool alarmActive, float timeSeconds)
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

    private struct SoundRipple(Vector2 origin, float maxRadius, Color baseColor, float age)
    {
        public Vector2 Origin { get; } = origin;
        public float MaxRadius { get; } = maxRadius;
        public Color BaseColor { get; } = baseColor;
        public float Age { get; set; } = age;
    }

    private struct GoldSpark(Vector3 position, Vector3 velocity, float age)
    {
        public Vector3 Position { get; set; } = position;
        public Vector3 Velocity { get; set; } = velocity;
        public float Age { get; set; } = age;
    }
}
